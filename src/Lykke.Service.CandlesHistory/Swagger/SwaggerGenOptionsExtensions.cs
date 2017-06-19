using Swashbuckle.SwaggerGen.Application;

namespace Lykke.Service.CandlesHistory.Swagger
{
    public static class SwaggerGenOptionsExtensions
    {
        public static void EnableXmsEnumExtension(this SwaggerGenOptions swaggerOptions, XmsEnumExtensionsOptions options = XmsEnumExtensionsOptions.UseEnums)
        {
            swaggerOptions.SchemaFilter<XmsEnumSchemaFilter>(options);
        }
    }
}