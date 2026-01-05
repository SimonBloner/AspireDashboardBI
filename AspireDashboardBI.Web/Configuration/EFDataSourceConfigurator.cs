using DevExpress.DashboardCommon;
using DevExpress.DashboardWeb;
using DevExpress.DataAccess.EntityFramework;

namespace AspireDashboardBI.Web.Configuration
{
    public class EFDataSourceConfigurator
    {
        public static void ConfigureDataSource(DataSourceInMemoryStorage storage)
        {
            // Registers an Entity Framework data source.
            DashboardEFDataSource efDataSource = new DashboardEFDataSource("EF Data Source");
            efDataSource.ConnectionParameters = new EFConnectionParameters(typeof(AppDbContext));
            storage.RegisterDataSource("efDataSource", efDataSource.SaveToXml());
        }
    }
}
