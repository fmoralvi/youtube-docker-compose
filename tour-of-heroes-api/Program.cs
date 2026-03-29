using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<HeroesContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () =>
{
    return Results.Text("Welcome to the Tour of Heroes API", "text/html");
});

app.MapGet("/api/hero", async (string? name, HeroesContext db) =>
{
    var query = db.Heroes.AsQueryable();
    if (!string.IsNullOrWhiteSpace(name))
        query = query.Where(h => h.Name.Contains(name));

    var heroes = await query.ToListAsync();
    return Results.Ok(heroes.Select(h => new Hero(h.Id, string.Format("{0} 🦸🏼‍♀️", h.Name), h.AlterEgo, h.Description)));
})
.WithName("GetHeroes")
.WithOpenApi();

app.MapGet("/api/hero/{id}", async (int id, HeroesContext db) =>
{
    var hero = await db.Heroes.FindAsync(id);
    if (hero is null) return Results.NotFound();
    return Results.Ok(new Hero(hero.Id, string.Format("{0} 🦸🏼‍♀️", hero.Name), hero.AlterEgo, hero.Description));
})
.WithName("GetHeroById")
.WithOpenApi();

app.MapPost("/api/hero", async (Hero hero, HeroesContext db) =>
{
    var newHero = hero with { AlterEgo = hero.AlterEgo ?? "", Description = hero.Description ?? "" };
    db.Heroes.Add(newHero);
    await db.SaveChangesAsync();
    return Results.Created($"/api/hero/{newHero.Id}", newHero);
})
.WithName("CreateHero")
.WithOpenApi();

app.MapPut("/api/hero/{id}", async (int id, Hero input, HeroesContext db) =>
{
    var hero = await db.Heroes.FindAsync(id);
    if (hero is null) return Results.NotFound();
    db.Entry(hero).CurrentValues.SetValues(input);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("UpdateHero")
.WithOpenApi();

app.MapDelete("/api/hero/{id}", async (int id, HeroesContext db) =>
{
    var hero = await db.Heroes.FindAsync(id);
    if (hero is null) return Results.NotFound();
    db.Heroes.Remove(hero);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteHero")
.WithOpenApi();

app.Run();


public record Hero(int Id, string Name, string? AlterEgo, string? Description);