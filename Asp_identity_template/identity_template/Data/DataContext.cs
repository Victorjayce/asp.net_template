using identity_template.Models;
using Microsoft.EntityFrameworkCore;

namespace identity_template.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        // Define your DbSets here...
        public DbSet<Users> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}
