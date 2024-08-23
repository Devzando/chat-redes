namespace chat.rede.webapi.config
{
    public static class CorsConfig
    {
        public static IServiceCollection AddCustomCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: "CorsPolicy",
                    options =>
                    {
                        options.WithOrigins("http://localhost:3000", "http://localhost:5500", "http://127.0.0.1:5500")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });

            return services;
        }
    }
}
