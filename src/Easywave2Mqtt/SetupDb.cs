namespace Easywave2Mqtt
{
  public static class SetupDb
  {
    public static void Prepare(IApplicationBuilder app)
    {
      using (IServiceScope? scope = app.ApplicationServices.CreateScope())
      {
        SeedData(scope.ServiceProvider.GetRequiredService<AppDbContext>());
      }
    }

    private static void SeedData(AppDbContext context)
    {

      if (context.Database.EnsureCreated())
      {
        if (context.Devices == null)
        {
          throw new ArgumentException("Devices DbSet is null", nameof(context));
        }
      }
      if (context.Devices.Any())
      {
        Console.WriteLine("Context has data");
        return;
      }
      Console.WriteLine("Seeding data");
      if (Program.Settings != null)
      {
        context.Devices.AddRange(Program.Settings.Devices);
      }
      _ = context.SaveChanges();
    }
  }
}
