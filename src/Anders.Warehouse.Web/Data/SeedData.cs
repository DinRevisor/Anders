using Anders.Warehouse.Web.Domain;
using Microsoft.AspNetCore.Identity;

namespace Anders.Warehouse.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        if (db.Tenants.Any()) return;

        var tenant = new Tenant { Name = "Demo Tenant" };
        db.Tenants.Add(tenant);

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { "Admin", "Manager", "Viewer" })
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole<Guid>(role));

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = new ApplicationUser { UserName = "admin@demo.local", Email = "admin@demo.local", TenantId = tenant.Id, EmailConfirmed = true };
        await userManager.CreateAsync(admin, "Passw0rd!");
        await userManager.AddToRoleAsync(admin, "Admin");

        await db.SaveChangesAsync();
    }
}
