﻿using IdentityServerAspNetIdentity.Data;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityServerAspNetIdentity
{
	public class Startup
	{
		public IWebHostEnvironment Environment { get; }
		public IConfiguration Configuration { get; }

		public Startup(IWebHostEnvironment environment, IConfiguration configuration)
		{
			Environment = environment;
			Configuration = configuration;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllersWithViews();

			// configures IIS out-of-proc settings (see https://github.com/aspnet/AspNetCore/issues/14882)
			services.Configure<IISOptions>(iis =>
			{
				iis.AuthenticationDisplayName = "Windows";
				iis.AutomaticAuthentication = false;
			});

			// configures IIS in-proc settings
			services.Configure<IISServerOptions>(iis =>
			{
				iis.AuthenticationDisplayName = "Windows";
				iis.AutomaticAuthentication = false;
			});

			services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

			services.AddIdentity<ApplicationUser, IdentityRole>()
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			var builder = services.AddIdentityServer(options =>
				{
					options.Events.RaiseErrorEvents = true;
					options.Events.RaiseInformationEvents = true;
					options.Events.RaiseFailureEvents = true;
					options.Events.RaiseSuccessEvents = true;
				})
				.AddInMemoryIdentityResources(Config.Ids)
				.AddInMemoryApiResources(Config.Apis)
				.AddInMemoryClients(Config.Clients)
				.AddAspNetIdentity<ApplicationUser>();

			// not recommended for production - you need to store your key material somewhere secure
			builder.AddDeveloperSigningCredential();

			services.AddAuthentication()
				.AddGoogle(options =>
				{
					// register your IdentityServer with Google at https://console.developers.google.com
					// enable the Google+ API
					// set the redirect URI to http://localhost:5000/signin-google
					options.ClientId = "copy client ID from Google here";
					options.ClientSecret = "copy client secret from Google here";
				});
		}

		public void Configure(IApplicationBuilder app)
		{
			UpdateDatabase(app);

			if (Environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
			}

			app.UseStaticFiles();

			app.UseRouting();
			app.UseIdentityServer();
			app.UseAuthorization();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapDefaultControllerRoute();
			});
		}

		private static void UpdateDatabase(IApplicationBuilder app)
		{
			using (var serviceScope = app.ApplicationServices
				.GetRequiredService<IServiceScopeFactory>()
				.CreateScope())
			{
				using (var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>())
				{
					context.Database.Migrate();
				}
			}
		}
	}
}