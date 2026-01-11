# Setup Guide

1. Clone the repository
2. Install dbmate https://github.com/amacneil/dbmate?tab=readme-ov-file#installation
3. Create .env files (Edit if desired)
```bash
cp .env.example .env
cp Tickflo.Web/.env.example Tickflo.Web/.env
```

4. Start the docker containers
```bash
cd Tickflo.Web
docker compose up -d
```

5. Apply database migrations (from repository root)
```bash
dbmate up
```

## Demo Workspace Reset (Scoped)
- The seed resets only data that this seed inserts (demo users and workspaces), without touching unrelated data or sequences.

Run after migrations:

```bash
psql -h localhost -U $POSTGRES_USER -d $POSTGRES_DB -f db/seed_data.sql

docker exec -i $(docker ps -qf name=db) psql -U $POSTGRES_USER -d $POSTGRES_DB -f /work/db/seed_data.sql
```

Notes:
- Only demo workspaces (`tickflo-demo`, `techstart`, `global-services`) and demo users are reset.
- Global permissions are upserted (no deletes); other workspace data remains intact.