Chạy các lệnh dưới trong terminal để thiết lập api db
cd MrpQuanLyThuoc.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=aws-0-ap-northeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.uhxginhmlnrikfyineei;Password=MRPQuanliThuoc"
