using System;
using Microsoft.AspNetCore.Identity;
using LMS_Backend.Models.Entities;

namespace PasswordHashTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var hasher = new PasswordHasher<User>();

            var password = "Admin123!";
            var user = new User();

            var hash = hasher.HashPassword(user, password);

            Console.WriteLine("Generated Bootstrap Admin Password Hash:");
            Console.WriteLine(hash);
        }
    }
}