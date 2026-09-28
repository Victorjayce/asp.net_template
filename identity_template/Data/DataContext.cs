using identity_template.Models;
using Microsoft.EntityFrameworkCore;

namespace identity_template.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        // Define your DbSets here...for now the default WeatherForecast
        DbSet<WeatherForecast> WeatherForecasts { get; set; }
        DbSet<Users> Users { get; set; }
    }
}
