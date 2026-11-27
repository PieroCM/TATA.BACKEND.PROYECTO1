using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;

namespace TATA.BACKEND.PROYECTO1.Tests.Unit;

/// <summary>
/// Pruebas del cálculo de SLA (SolicitudService.ActualizarSlaDiarioAsync).
/// Se usa una fecha de referencia fija, así el resultado no depende del día en que se ejecuten.
/// </summary>
public class SlaCalculoTests
{
    private static readonly DateTime Hoy = new(2026, 11, 13);

    private static ConfigSla Sla(int diasUmbral, string codigo = "SLA1") =>
        new() { IdSla = 1, CodigoSla = codigo, DiasUmbral = diasUmbral, TipoSolicitud = "NUEVO_INGRESO" };

    private static Solicitud SolicitudCon(ConfigSla sla, DateTime fechaSolicitud, DateTime? fechaIngreso = null) =>
        new()
        {
            IdSolicitud = 10,
            IdSla = sla.IdSla,
            IdSlaNavigation = sla,
            FechaSolicitud = DateOnly.FromDateTime(fechaSolicitud),
            FechaIngreso = fechaIngreso.HasValue ? DateOnly.FromDateTime(fechaIngreso.Value) : null,
            EstadoSolicitud = "SIN_CALCULAR"
        };

    /// <summary>Ejecuta el recálculo diario sobre una sola solicitud y devuelve el repositorio simulado.</summary>
    private static async Task<(int actualizadas, Mock<ISolicitudRepository> repo)> Recalcular(Solicitud solicitud)
    {
        var repo = new Mock<ISolicitudRepository>();
        repo.Setup(r => r.GetSolicitudesParaRecalculoAsync()).ReturnsAsync(new List<Solicitud> { solicitud });
        repo.Setup(r => r.UpdateSolicitudAsync(It.IsAny<int>(), It.IsAny<Solicitud>()))
            .ReturnsAsync((int _, Solicitud s) => s);

        var service = new SolicitudService(repo.Object, Mock.Of<IAlertaRepository>(),
            NullLogger<SolicitudService>.Instance);

        var actualizadas = await service.ActualizarSlaDiarioAsync(Hoy);
        return (actualizadas, repo);
    }

    [Fact]
    public async Task SinFechaIngreso_DentroDelUmbral_QuedaEnProceso()
    {
        var solicitud = SolicitudCon(Sla(diasUmbral: 5), Hoy.AddDays(-3));

        var (actualizadas, _) = await Recalcular(solicitud);

        Assert.Equal(1, actualizadas);
        Assert.Equal(3, solicitud.NumDiasSla);
        Assert.Equal("EN_PROCESO", solicitud.EstadoSolicitud);
        Assert.Equal("EN_PROCESO_SLA1", solicitud.EstadoCumplimientoSla);
    }

    [Fact]
    public async Task SinFechaIngreso_JustoEnElUmbral_TodaviaNoVence()
    {
        // 5 días transcurridos con umbral de 5: el SLA vence recién al superar el umbral
        var solicitud = SolicitudCon(Sla(diasUmbral: 5), Hoy.AddDays(-5));

        await Recalcular(solicitud);

        Assert.Equal(5, solicitud.NumDiasSla);
        Assert.Equal("EN_PROCESO", solicitud.EstadoSolicitud);
    }

    [Fact]
    public async Task SinFechaIngreso_SuperaElUmbral_QuedaVencida()
    {
        var solicitud = SolicitudCon(Sla(diasUmbral: 5), Hoy.AddDays(-8));

        await Recalcular(solicitud);

        Assert.Equal(8, solicitud.NumDiasSla);
        Assert.Equal("VENCIDA", solicitud.EstadoSolicitud);
        Assert.Equal("NO_CUMPLE_SLA1", solicitud.EstadoCumplimientoSla);
    }

    [Fact]
    public async Task ConFechaIngreso_DentroDelUmbral_CumpleYQuedaInactiva()
    {
        var fechaSolicitud = Hoy.AddDays(-20);
        var solicitud = SolicitudCon(Sla(diasUmbral: 5), fechaSolicitud, fechaIngreso: fechaSolicitud.AddDays(4));

        await Recalcular(solicitud);

        Assert.Equal(4, solicitud.NumDiasSla);
        Assert.Equal("INACTIVA", solicitud.EstadoSolicitud);
        Assert.Equal("CUMPLE_SLA1", solicitud.EstadoCumplimientoSla);
    }

    [Fact]
    public async Task ConFechaIngreso_FueraDelUmbral_NoCumple()
    {
        var fechaSolicitud = Hoy.AddDays(-20);
        var solicitud = SolicitudCon(Sla(diasUmbral: 5, codigo: "SLA2"), fechaSolicitud, fechaIngreso: fechaSolicitud.AddDays(9));

        await Recalcular(solicitud);

        Assert.Equal(9, solicitud.NumDiasSla);
        Assert.Equal("VENCIDA", solicitud.EstadoSolicitud);
        Assert.Equal("NO_CUMPLE_SLA2", solicitud.EstadoCumplimientoSla);
    }

    [Fact]
    public async Task FechaIngresoAnteriorALaSolicitud_NoSeActualiza()
    {
        // Dato inválido: el servicio lo registra como error y sigue, sin guardar nada
        var solicitud = SolicitudCon(Sla(diasUmbral: 5), Hoy.AddDays(-2), fechaIngreso: Hoy.AddDays(-6));

        var (actualizadas, repo) = await Recalcular(solicitud);

        Assert.Equal(0, actualizadas);
        repo.Verify(r => r.UpdateSolicitudAsync(It.IsAny<int>(), It.IsAny<Solicitud>()), Times.Never);
    }
}
