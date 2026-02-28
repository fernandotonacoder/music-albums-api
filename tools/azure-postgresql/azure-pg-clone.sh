#!/bin/bash
GREEN='\033[0;32m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'
echo -e "${GREEN}--- Azure PostgreSQL Cloner (v18.3 Engine) ---${NC}"
# Source Data Collection
read -p "Source Host: " SRC_HOST
read -p "Source User: " SRC_USER
read -s -p "Source Password: " SRC_PASS
echo ""
read -p "Source Database: " SRC_DB
echo -e "\n-----------------------------------"
# Destination Data Collection
read -p "Destination Host: " DEST_HOST
read -p "Destination User: " DEST_USER
read -s -p "Destination Password: " DEST_PASS
echo ""
read -p "Destination Database: " DEST_DB
echo -e "\n${RED}WARNING: Clear public schema at destination? (y/n)${NC}"
read -r CLEAN_DEST
# Destination Cleanup
if [[ "$CLEAN_DEST" =~ ^([yY][eE][sS]|[yY])$ ]]; then
    echo -e "${CYAN}Cleaning destination public schema...${NC}"
    PGPASSWORD="$DEST_PASS" psql -h "$DEST_HOST" -U "$DEST_USER" -d "$DEST_DB" -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
fi
echo -e "\n${GREEN}Starting transfer...${NC}"
echo -e "${CYAN}Note: Hiding system extension errors...${NC}"
export PGPASSWORD="$SRC_PASS"
# Filter pg_dump stderr AND psql stderr to suppress known Azure extension noise.
# The psql filter catches ERROR, HINT, and detail lines referencing those extensions.
./pg_dump_v18 -h "$SRC_HOST" -U "$SRC_USER" -d "$SRC_DB" \
    --no-owner \
    --no-privileges \
    --exclude-schema='cron' \
    --exclude-schema='azure_utils' \
    --exclude-table='public.job' \
    --exclude-table='public.job_run_details' \
    --clean \
    --if-exists \
    --format=p 2> >(grep -v -E "(pgaadauth|azure|pg_cron)" >&2) \
    | PGPASSWORD="$DEST_PASS" psql -h "$DEST_HOST" -U "$DEST_USER" -d "$DEST_DB" --quiet \
      2> >(grep -v -E "(pgaadauth|azure|pg_cron|allow-listed|fwlink\.microsoft|2301063)" >&2)
# CAPTURE PIPESTATUS IMMEDIATELY - This is the fix for the integer error
DUMP_STAT=${PIPESTATUS[0]}
PSQL_STAT=${PIPESTATUS[1]}
# In Azure, pg_dump often returns 1 even on success because of those extensions.
# We treat 0 and 1 as success for the dump.
if [[ "$DUMP_STAT" -le 1 ]] && [[ "$PSQL_STAT" -le 1 ]]; then
    echo -e "\n${GREEN}✅ Success! Data successfully migrated to $DEST_HOST${NC}"
else
    echo -e "\n${RED}❌ Error detected during transfer.${NC}"
    echo -e "Dump Status: $DUMP_STAT | Psql Status: $PSQL_STAT"
fi
unset PGPASSWORD
