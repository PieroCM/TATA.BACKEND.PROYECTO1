using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.Tests.Unit;

/// <summary>
/// Pruebas de los niveles de alerta que asigna AlertaService.SyncAlertasFromSolicitudesAsync
/// según los días que le quedan a cada solicitud antes de vencer su SLA.
/// </summary>
public class NivelAlertaTests
{
    private static Solicitud SolicitudActiva(int idSolicitud, int diasUmbral, int diasTranscurridos, string estado = "EN_PROCESO") =>
        new()
        {
            IdSolicitud = idSolicitud,
            EstadoSolicitud = estado,
            FechaSolicitud = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-diasTranscurridos)),
            IdSlaNavigation = new ConfigSla { IdSla = 1, CodigoSla = "SLA1", DiasUmbral = diasUmbral, TipoSolicitud = "NUEVO_INGRESO" }
        };

    /// <summary>Sincroniza las solicitudes dadas y devuelve las alertas que el servicio intentó crear.</summary>
    private static async Task<List<Alerta>> Sincronizar(params Solicitud[] solicitudes)
    {
        var creadas = new List<Alerta>();

        var solicitudRepo = new Mock<ISolicitudRepository>();
        solicitudRepo.Setup(r => r.GetSolicitudsAsync()).ReturnsAsync(solicitudes.ToList());

        var alertaRepo = new Mock<IAlertaRepository>();
        alertaRepo.Setup(r => r.GetAlertaBySolicitudIdAsync(It.IsAny<int>())).ReturnsAsync((Alerta?)null);
        alertaRepo.Setup(r => r.CreateAlertaAsync(It.IsAny<Alerta>()))
            .Callback<Alerta>(creadas.Add)
            .ReturnsAsync((Alerta a) => a);

        // La sincronización no consulta el DbContext, pero el servicio lo pide en el constructor
        var options = new DbContextOptionsBuilder<Proyecto1SlaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new Proyecto1SlaDbContext(options);

        var service = new AlertaService(alertaRepo.Object, Mock.Of<IEmailService>(), solicitudRepo.Object,
            context, NullLogger<AlertaService>.Instance);

        await service.SyncAlertasFromSolicitudesAsync();
        return creadas;
    }

    [Theory]
    // umbral, días transcurridos -> días restantes = umbral - transcurridos
    [InlineData(5, 8, "CRITICO", "SLA_VENCIDO")]                 // -3: ya venció
    [InlineData(5, 4, "CRITICO", "SLA_VENCIMIENTO_INMEDIATO")]   //  1: vence en 2 días o menos
    [InlineData(10, 6, "ALTO", "SLA_PREVENTIVA")]                //  4: entre 3 y 5 días
    [InlineData(10, 2, "MEDIO", "SLA_SEGUIMIENTO")]              //  8: entre 6 y 10 días
    [InlineData(30, 1, "BAJO", "SLA_NORMAL")]                    // 29: más de 10 días
    public async Task AsignaNivelSegunDiasRestantes(int diasUmbral, int diasTranscurridos, string nivelEsperado, string tipoEsperado)
    {
        var creadas = await Sincronizar(SolicitudActiva(1, diasUmbral, diasTranscurridos));

        var alerta = Assert.Single(creadas);
        Assert.Equal(nivelEsperado, alerta.Nivel);
        Assert.Equal(tipoEsperado, alerta.TipoAlerta);
        Assert.Equal("ACTIVA", alerta.Estado);
        Assert.False(alerta.EnviadoEmail);
    }

    [Fact]
    public async Task SolicitudesCerradasOEliminadas_NoGeneranAlerta()
    {
        var creadas = await Sincronizar(
            SolicitudActiva(1, diasUmbral: 5, diasTranscurridos: 8, estado: "CERRADO"),
            SolicitudActiva(2, diasUmbral: 5, diasTranscurridos: 8, estado: "ELIMINADO"),
            SolicitudActiva(3, diasUmbral: 5, diasTranscurridos: 1));

        var alerta = Assert.Single(creadas);
        Assert.Equal(3, alerta.IdSolicitud);
    }
}
