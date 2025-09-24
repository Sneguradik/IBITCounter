using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class PosttradeDbContext(DbContextOptions<PosttradeDbContext> options) : DbContext(options);