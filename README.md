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