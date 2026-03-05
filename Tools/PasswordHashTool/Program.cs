using System;
using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<User>();

const string password = "Admin123!";
var user = new User();

var hash = hasher.HashPassword(user, password);

Console.WriteLine("Generated Bootstrap Admin Password Hash:");
Console.WriteLine(hash);
