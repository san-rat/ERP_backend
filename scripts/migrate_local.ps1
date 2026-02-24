# scripts/migrate-local.ps1
docker exec -i erp-mysql-local mysql -u auth_user -pauth_password auth_db < db/auth/migrations/001_init.sql
echo "Local migration applied"