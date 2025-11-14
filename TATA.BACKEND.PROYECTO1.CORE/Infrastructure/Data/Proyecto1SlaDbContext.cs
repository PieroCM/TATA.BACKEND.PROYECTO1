using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

public partial class Proyecto1SlaDbContext : DbContext
{
    public Proyecto1SlaDbContext()
    {
    }

    public Proyecto1SlaDbContext(DbContextOptions<Proyecto1SlaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alerta> Alerta { get; set; }

    public virtual DbSet<ConfigSla> ConfigSla { get; set; }

    public virtual DbSet<LogSistema> LogSistema { get; set; }

    public virtual DbSet<Permiso> Permiso { get; set; }

    public virtual DbSet<Personal> Personal { get; set; }

    public virtual DbSet<Reporte> Reporte { get; set; }

    public virtual DbSet<RolRegistro> RolRegistro { get; set; }

    public virtual DbSet<RolesSistema> RolesSistema { get; set; }

    public virtual DbSet<Solicitud> Solicitud { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }
    //Para el reporte detalle
    public virtual DbSet<ReporteDetalle> ReporteDetalle { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alerta>(entity =>
        {
            entity.HasKey(e => e.IdAlerta);

            entity.ToTable("alerta");

            entity.HasIndex(e => e.Estado, "IX_alerta_estado");

            entity.HasIndex(e => e.IdSolicitud, "IX_alerta_solicitud");

            entity.HasIndex(e => new { e.TipoAlerta, e.Nivel }, "IX_alerta_tipo");

            entity.Property(e => e.IdAlerta).HasColumnName("id_alerta");
            entity.Property(e => e.ActualizadoEn).HasColumnName("actualizado_en");
            entity.Property(e => e.EnviadoEmail).HasColumnName("enviado_email");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasColumnName("estado");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaLectura).HasColumnName("fecha_lectura");
            entity.Property(e => e.IdSolicitud).HasColumnName("id_solicitud");
            entity.Property(e => e.Mensaje).HasColumnName("mensaje");
            entity.Property(e => e.Nivel)
                .HasMaxLength(20)
                .HasColumnName("nivel");
            entity.Property(e => e.TipoAlerta)
                .HasMaxLength(40)
                .HasColumnName("tipo_alerta");

            entity.HasOne(d => d.IdSolicitudNavigation).WithMany(p => p.Alerta)
                .HasForeignKey(d => d.IdSolicitud)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_alerta_solicitud");
        });

        modelBuilder.Entity<ConfigSla>(entity =>
        {
            entity.HasKey(e => e.IdSla);

            entity.ToTable("config_sla");

            entity.HasIndex(e => e.EsActivo, "IX_config_sla_activo");

            entity.HasIndex(e => e.TipoSolicitud, "IX_config_sla_tipo");

            entity.HasIndex(e => e.CodigoSla, "UX_config_sla_codigo").IsUnique();

            entity.Property(e => e.IdSla).HasColumnName("id_sla");
            entity.Property(e => e.ActualizadoEn).HasColumnName("actualizado_en");
            entity.Property(e => e.CodigoSla)
                .HasMaxLength(50)
                .HasColumnName("codigo_sla");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .HasColumnName("descripcion");
            entity.Property(e => e.DiasUmbral).HasColumnName("dias_umbral");
            entity.Property(e => e.EsActivo)
                .HasDefaultValue(true)
                .HasColumnName("es_activo");
            entity.Property(e => e.TipoSolicitud)
                .HasMaxLength(20)
                .HasColumnName("tipo_solicitud");
        });

        modelBuilder.Entity<LogSistema>(entity =>
        {
            entity.HasKey(e => e.IdLog);

            entity.ToTable("log_sistema");

            entity.HasIndex(e => e.FechaHora, "IX_log_fecha").IsDescending();

            entity.HasIndex(e => e.Nivel, "IX_log_nivel");

            entity.HasIndex(e => e.IdUsuario, "IX_log_usuario");

            entity.Property(e => e.IdLog).HasColumnName("id_log");
            entity.Property(e => e.Detalles).HasColumnName("detalles");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Mensaje)
                .HasMaxLength(500)
                .HasColumnName("mensaje");
            entity.Property(e => e.Nivel)
                .HasMaxLength(20)
                .HasColumnName("nivel");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.LogSistema)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_log_usuario");
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.HasKey(e => e.IdPermiso);

            entity.ToTable("permiso");

            entity.HasIndex(e => e.Codigo, "UX_permiso_codigo").IsUnique();

            entity.Property(e => e.IdPermiso).HasColumnName("id_permiso");
            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .HasColumnName("codigo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(120)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Personal>(entity =>
        {
            entity.HasKey(e => e.IdPersonal);

            entity.ToTable("personal");

            entity.HasIndex(e => e.CorreoCorporativo, "IX_personal_correoCorp");

            entity.HasIndex(e => e.Documento, "IX_personal_documento");

            entity.HasIndex(e => e.Estado, "IX_personal_estado");

            entity.HasIndex(e => e.IdUsuario, "IX_personal_usuario");

            entity.Property(e => e.IdPersonal).HasColumnName("id_personal");
            entity.Property(e => e.ActualizadoEn).HasColumnName("actualizado_en");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(120)
                .HasColumnName("apellidos");
            entity.Property(e => e.CorreoCorporativo)
                .HasMaxLength(190)
                .HasColumnName("correo_corporativo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.Documento)
                .HasMaxLength(20)
                .HasColumnName("documento");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasColumnName("estado");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Nombres)
                .HasMaxLength(120)
                .HasColumnName("nombres");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Personal)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_personal_usuario");
        });

        modelBuilder.Entity<Reporte>(entity =>
        {
            entity.HasKey(e => e.IdReporte);

            entity.ToTable("reporte");

            entity.HasIndex(e => new { e.GeneradoPor, e.FechaGeneracion }, "IX_reporte_generado").IsDescending(false, true);

            entity.HasIndex(e => new { e.TipoReporte, e.Formato }, "IX_reporte_tipo_formato");

            entity.Property(e => e.IdReporte).HasColumnName("id_reporte");
            entity.Property(e => e.FechaGeneracion)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("fecha_generacion");
            entity.Property(e => e.FiltrosJson).HasColumnName("filtros_json");
            entity.Property(e => e.Formato)
                .HasMaxLength(10)
                .HasColumnName("formato");
            entity.Property(e => e.GeneradoPor).HasColumnName("generado_por");
            entity.Property(e => e.RutaArchivo)
                .HasMaxLength(400)
                .HasColumnName("ruta_archivo");
            entity.Property(e => e.TipoReporte)
                .HasMaxLength(40)
                .HasColumnName("tipo_reporte");

            entity.HasOne(d => d.GeneradoPorNavigation).WithMany(p => p.Reporte)
                .HasForeignKey(d => d.GeneradoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reporte_usuario");

            
        });

        modelBuilder.Entity<RolRegistro>(entity =>
        {
            entity.HasKey(e => e.IdRolRegistro);

            entity.ToTable("rol_registro");

            entity.HasIndex(e => e.EsActivo, "IX_rol_registro_activo");

            entity.HasIndex(e => e.BloqueTech, "IX_rol_registro_bloque");

            entity.HasIndex(e => e.NombreRol, "UX_rol_registro_nombre").IsUnique();

            entity.Property(e => e.IdRolRegistro).HasColumnName("id_rol_registro");
            entity.Property(e => e.BloqueTech)
                .HasMaxLength(80)
                .HasColumnName("bloque_tech");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .HasColumnName("descripcion");
            entity.Property(e => e.EsActivo)
                .HasDefaultValue(true)
                .HasColumnName("es_activo");
            entity.Property(e => e.NombreRol)
                .HasMaxLength(120)
                .HasColumnName("nombre_rol");
        });

        modelBuilder.Entity<RolesSistema>(entity =>
        {
            entity.HasKey(e => e.IdRolSistema);

            entity.ToTable("roles_sistema");

            entity.HasIndex(e => e.EsActivo, "IX_roles_sistema_activo");

            entity.HasIndex(e => e.Codigo, "UX_roles_sistema_codigo").IsUnique();

            entity.Property(e => e.IdRolSistema).HasColumnName("id_rol_sistema");
            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .HasColumnName("codigo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .HasColumnName("descripcion");
            entity.Property(e => e.EsActivo)
                .HasDefaultValue(true)
                .HasColumnName("es_activo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(120)
                .HasColumnName("nombre");

            entity.HasMany(d => d.IdPermiso).WithMany(p => p.IdRolSistema)
                .UsingEntity<Dictionary<string, object>>(
                    "RolPermiso",
                    r => r.HasOne<Permiso>().WithMany()
                        .HasForeignKey("IdPermiso")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_rol_permiso_perm"),
                    l => l.HasOne<RolesSistema>().WithMany()
                        .HasForeignKey("IdRolSistema")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_rol_permiso_rol"),
                    j =>
                    {
                        j.HasKey("IdRolSistema", "IdPermiso");
                        j.ToTable("rol_permiso");
                        j.HasIndex(new[] { "IdPermiso" }, "IX_rol_permiso_perm");
                        j.IndexerProperty<int>("IdRolSistema").HasColumnName("id_rol_sistema");
                        j.IndexerProperty<int>("IdPermiso").HasColumnName("id_permiso");
                    });
        });

        modelBuilder.Entity<Solicitud>(entity =>
        {
            entity.HasKey(e => e.IdSolicitud);

            entity.ToTable("solicitud");

            entity.HasIndex(e => e.CreadoPor, "IX_solicitud_creado_por");

            entity.HasIndex(e => e.EstadoSolicitud, "IX_solicitud_estado");

            entity.HasIndex(e => new { e.FechaSolicitud, e.FechaIngreso }, "IX_solicitud_fechas");

            entity.HasIndex(e => e.IdPersonal, "IX_solicitud_personal");

            entity.HasIndex(e => e.IdRolRegistro, "IX_solicitud_rol_registro");

            entity.HasIndex(e => e.IdSla, "IX_solicitud_sla");

            entity.Property(e => e.IdSolicitud).HasColumnName("id_solicitud");
            entity.Property(e => e.ActualizadoEn).HasColumnName("actualizado_en");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por");
            entity.Property(e => e.EstadoCumplimientoSla)
                .HasMaxLength(30)
                .HasColumnName("estado_cumplimiento_sla");
            entity.Property(e => e.EstadoSolicitud)
                .HasMaxLength(30)
                .HasColumnName("estado_solicitud");
            entity.Property(e => e.FechaIngreso).HasColumnName("fecha_ingreso");
            entity.Property(e => e.FechaSolicitud).HasColumnName("fecha_solicitud");
            entity.Property(e => e.IdPersonal).HasColumnName("id_personal");
            entity.Property(e => e.IdRolRegistro).HasColumnName("id_rol_registro");
            entity.Property(e => e.IdSla).HasColumnName("id_sla");
            entity.Property(e => e.NumDiasSla).HasColumnName("num_dias_sla");
            entity.Property(e => e.OrigenDato)
                .HasMaxLength(40)
                .HasColumnName("origen_dato");
            entity.Property(e => e.ResumenSla)
                .HasMaxLength(300)
                .HasColumnName("resumen_sla");

            entity.HasOne(d => d.CreadoPorNavigation).WithMany(p => p.Solicitud)
                .HasForeignKey(d => d.CreadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_solicitud_creado_por");

            entity.HasOne(d => d.IdPersonalNavigation).WithMany(p => p.Solicitud)
                .HasForeignKey(d => d.IdPersonal)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_solicitud_personal");

            entity.HasOne(d => d.IdRolRegistroNavigation).WithMany(p => p.Solicitud)
                .HasForeignKey(d => d.IdRolRegistro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_solicitud_rol_registro");

            entity.HasOne(d => d.IdSlaNavigation).WithMany(p => p.Solicitud)
                .HasForeignKey(d => d.IdSla)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_solicitud_sla");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.ToTable("usuario");

            entity.HasIndex(e => e.Estado, "IX_usuario_estado");

            entity.HasIndex(e => e.IdRolSistema, "IX_usuario_rol");

            entity.HasIndex(e => e.Correo, "UX_usuario_correo").IsUnique();

            entity.HasIndex(e => e.Username, "UX_usuario_username").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.ActualizadoEn).HasColumnName("actualizado_en");
            entity.Property(e => e.Correo)
                .HasMaxLength(190)
                .HasColumnName("correo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasColumnName("estado");
            entity.Property(e => e.IdRolSistema).HasColumnName("id_rol_sistema");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UltimoLogin).HasColumnName("ultimo_login");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.HasOne(d => d.IdRolSistemaNavigation).WithMany(p => p.Usuario)
                .HasForeignKey(d => d.IdRolSistema)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_usuario_rol");
        });

        // Configuración many-to-many entre Reporte y Solicitud usando la entidad explícita ReporteDetalle
        modelBuilder.Entity<Reporte>()
            .HasMany(r => r.Solicitudes)
            .WithMany(s => s.IdReporte)
            .UsingEntity<ReporteDetalle>(
                // relación desde ReporteDetalle hacia Solicitud
                j => j.HasOne(rd => rd.Solicitud)
                      .WithMany() // no hay colección de ReporteDetalle en Solicitud
                      .HasForeignKey(rd => rd.IdSolicitud)
                      .OnDelete(DeleteBehavior.ClientSetNull)
                      .HasConstraintName("FK_repdet_solicitud"),
                // relación desde ReporteDetalle hacia Reporte
                j => j.HasOne(rd => rd.Reporte)
                      .WithMany(r => r.Detalles)
                      .HasForeignKey(rd => rd.IdReporte)
                      .OnDelete(DeleteBehavior.ClientSetNull)
                      .HasConstraintName("FK_repdet_reporte"),
                // configuración de la tabla de unión
                j =>
                {
                    j.HasKey(rd => new { rd.IdReporte, rd.IdSolicitud }).HasName("PK_reporte_detalle");
                    j.ToTable("reporte_detalle");
                    j.HasIndex(rd => rd.IdSolicitud, "IX_repdet_solicitud");

                    j.Property(rd => rd.IdReporte).HasColumnName("id_reporte");
                    j.Property(rd => rd.IdSolicitud).HasColumnName("id_solicitud");
                });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
