using Microsoft.EntityFrameworkCore;

namespace Server.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{

}
