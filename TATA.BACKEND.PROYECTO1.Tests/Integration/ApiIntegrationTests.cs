using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TATA.BACKEND.PROYECTO1.Tests.Integration;

/// <summary>
/// Pruebas de integración: recorren el pipeline HTTP real de la API
/// (routing, autenticación JWT, controladores, servicios y EF Core).
/// </summary>
public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiIntegrationTests(ApiFactory factory) => _factory = factory;

    private async Task<HttpResponseMessage> Login(HttpClient client, string password) =>
        await client.PostAsJsonAsync("/api/usuario/signin", new { email = ApiFactory.AdminEmail, password });

    [Fact]
    public async Task Health_RespondeHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_ConCredencialesDelSeeder_DevuelveToken()
    {
        var client = _factory.CreateClient();

        var response = await Login(client, ApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.Equal("SUPER_ADMIN", body.GetProperty("rolCodigo").GetString());
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_Devuelve401()
    {
        var client = _factory.CreateClient();

        var response = await Login(client, "ContrasenaEquivocada1!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointsDeNegocio_SinToken_Devuelven401()
    {
        // Antes de agregar [Authorize] estos endpoints respondían datos a cualquiera
        var client = _factory.CreateClient();

        foreach (var ruta in new[] { "/api/solicitud", "/api/alertas/dashboard", "/api/rolessistema", "/api/configsla" })
        {
            var response = await client.GetAsync(ruta);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
                $"{ruta} respondió {(int)response.StatusCode} sin token; se esperaba 401");
        }
    }

    [Fact]
    public async Task RolesSistema_ConToken_DevuelveLosRolesSembrados()
    {
        var client = _factory.CreateClient();
        var login = await (await Login(client, ApiFactory.AdminPassword)).Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.GetProperty("token").GetString());

        var response = await client.GetAsync("/api/rolessistema");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        foreach (var rol in new[] { "SUPER_ADMIN", "ADMIN", "ANALISTA", "ESPECIALISTA" })
        {
            Assert.Contains(rol, json);
        }
    }
}
