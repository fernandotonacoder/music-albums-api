# Custom PostgreSQL Cloning Tool for Azure Cloud Shell

`azure-pg-clone.sh` is an interactive bash script that automates database cloning between Azure PostgreSQL Flexible Server instances. Handles schema cleanup, pg_dump/psql piping, and suppresses known Azure system extension noise while preserving real error visibility.

## Usage

1. Go to the Azure Portal
2. Open Cloud Shell
3. Upload both the `azure-pg-clone.sh` and `pg_dump_v18` files
4. `chmod +x azure-pg-clone.sh`
5. `chmod +x pg_dump_v18`
6. `./azure-pg-clone.sh`
7. Follow the instructions on the terminal