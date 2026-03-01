using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace LMS_Backend.Data;

public class ApplicationDBContext : IdentityDbContext<User>
{
    public ApplicationDBContext(DbContextOptions options) : base(options)
    {
    }
}