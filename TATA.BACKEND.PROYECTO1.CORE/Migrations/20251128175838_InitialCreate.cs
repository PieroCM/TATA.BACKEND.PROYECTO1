using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TATA.BACKEND.PROYECTO1.CORE.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "config_sla",
                columns: table => new
                {
                    id_sla = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_sla = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    dias_umbral = table.Column<int>(type: "int", nullable: false),
                    tipo_solicitud = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    es_activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_sla", x => x.id_sla);
                });

            migrationBuilder.CreateTable(
                name: "email_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    destinatario_resumen = table.Column<string>(type: "nvarchar(190)", maxLength: 190, nullable: false),
                    envio_inmediato = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    resumen_diario = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    hora_resumen = table.Column<TimeSpan>(type: "time", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())"),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    destinatarios = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    error_detalle = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permiso",
                columns: table => new
                {
                    id_permiso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permiso", x => x.id_permiso);
                });

            migrationBuilder.CreateTable(
                name: "personal",
                columns: table => new
                {
                    id_personal = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombres = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    apellidos = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    correo_corporativo = table.Column<string>(type: "nvarchar(190)", maxLength: 190, nullable: true),
                    documento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal", x => x.id_personal);
                });

            migrationBuilder.CreateTable(
                name: "rol_registro",
                columns: table => new
                {
                    id_rol_registro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre_rol = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    bloque_tech = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    es_activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol_registro", x => x.id_rol_registro);
                });

            migrationBuilder.CreateTable(
                name: "roles_sistema",
                columns: table => new
                {
                    id_rol_sistema = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    es_activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles_sistema", x => x.id_rol_sistema);
                });

            migrationBuilder.CreateTable(
                name: "rol_permiso",
                columns: table => new
                {
                    id_rol_sistema = table.Column<int>(type: "int", nullable: false),
                    id_permiso = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol_permiso", x => new { x.id_rol_sistema, x.id_permiso });
                    table.ForeignKey(
                        name: "FK_rol_permiso_perm",
                        column: x => x.id_permiso,
                        principalTable: "permiso",
                        principalColumn: "id_permiso");
                    table.ForeignKey(
                        name: "FK_rol_permiso_rol",
                        column: x => x.id_rol_sistema,
                        principalTable: "roles_sistema",
                        principalColumn: "id_rol_sistema");
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    id_rol_sistema = table.Column<int>(type: "int", nullable: false),
                    id_personal = table.Column<int>(type: "int", nullable: true),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ultimo_login = table.Column<DateTime>(type: "datetime2", nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    token_recuperacion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    expiracion_token = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id_usuario);
                    table.ForeignKey(
                        name: "FK_usuario_personal",
                        column: x => x.id_personal,
                        principalTable: "personal",
                        principalColumn: "id_personal",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_usuario_rol",
                        column: x => x.id_rol_sistema,
                        principalTable: "roles_sistema",
                        principalColumn: "id_rol_sistema");
                });

            migrationBuilder.CreateTable(
                name: "log_sistema",
                columns: table => new
                {
                    id_log = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fecha_hora = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    nivel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    detalles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_sistema", x => x.id_log);
                    table.ForeignKey(
                        name: "FK_log_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "reporte",
                columns: table => new
                {
                    id_reporte = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo_reporte = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    formato = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    filtros_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ruta_archivo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    generado_por = table.Column<int>(type: "int", nullable: false),
                    fecha_generacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporte", x => x.id_reporte);
                    table.ForeignKey(
                        name: "FK_reporte_usuario",
                        column: x => x.generado_por,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "solicitud",
                columns: table => new
                {
                    id_solicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_personal = table.Column<int>(type: "int", nullable: false),
                    id_sla = table.Column<int>(type: "int", nullable: false),
                    id_rol_registro = table.Column<int>(type: "int", nullable: false),
                    creado_por = table.Column<int>(type: "int", nullable: false),
                    fecha_solicitud = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_ingreso = table.Column<DateOnly>(type: "date", nullable: true),
                    num_dias_sla = table.Column<int>(type: "int", nullable: true),
                    resumen_sla = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    origen_dato = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    estado_solicitud = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    estado_cumplimiento_sla = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitud", x => x.id_solicitud);
                    table.ForeignKey(
                        name: "FK_solicitud_creado_por",
                        column: x => x.creado_por,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                    table.ForeignKey(
                        name: "FK_solicitud_personal",
                        column: x => x.id_personal,
                        principalTable: "personal",
                        principalColumn: "id_personal");
                    table.ForeignKey(
                        name: "FK_solicitud_rol_registro",
                        column: x => x.id_rol_registro,
                        principalTable: "rol_registro",
                        principalColumn: "id_rol_registro");
                    table.ForeignKey(
                        name: "FK_solicitud_sla",
                        column: x => x.id_sla,
                        principalTable: "config_sla",
                        principalColumn: "id_sla");
                });

            migrationBuilder.CreateTable(
                name: "alerta",
                columns: table => new
                {
                    id_alerta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_solicitud = table.Column<int>(type: "int", nullable: false),
                    tipo_alerta = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    nivel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    enviado_email = table.Column<bool>(type: "bit", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    fecha_lectura = table.Column<DateTime>(type: "datetime2", nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerta", x => x.id_alerta);
                    table.ForeignKey(
                        name: "FK_alerta_solicitud",
                        column: x => x.id_solicitud,
                        principalTable: "solicitud",
                        principalColumn: "id_solicitud");
                });

            migrationBuilder.CreateTable(
                name: "reporte_detalle",
                columns: table => new
                {
                    id_reporte = table.Column<int>(type: "int", nullable: false),
                    id_solicitud = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporte_detalle", x => new { x.id_reporte, x.id_solicitud });
                    table.ForeignKey(
                        name: "FK_repdet_reporte",
                        column: x => x.id_reporte,
                        principalTable: "reporte",
                        principalColumn: "id_reporte");
                    table.ForeignKey(
                        name: "FK_repdet_solicitud",
                        column: x => x.id_solicitud,
                        principalTable: "solicitud",
                        principalColumn: "id_solicitud");
                });

            migrationBuilder.InsertData(
                table: "email_config",
                columns: new[] { "id", "actualizado_en", "destinatario_resumen", "envio_inmediato", "hora_resumen" },
                values: new object[] { 1, null, "22200150@ue.edu.pe", true, new TimeSpan(0, 8, 0, 0, 0) });

            migrationBuilder.CreateIndex(
                name: "IX_alerta_estado",
                table: "alerta",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_alerta_solicitud",
                table: "alerta",
                column: "id_solicitud");

            migrationBuilder.CreateIndex(
                name: "IX_alerta_tipo",
                table: "alerta",
                columns: new[] { "tipo_alerta", "nivel" });

            migrationBuilder.CreateIndex(
                name: "IX_config_sla_activo",
                table: "config_sla",
                column: "es_activo");

            migrationBuilder.CreateIndex(
                name: "IX_config_sla_tipo",
                table: "config_sla",
                column: "tipo_solicitud");

            migrationBuilder.CreateIndex(
                name: "UX_config_sla_codigo",
                table: "config_sla",
                column: "codigo_sla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_log_estado",
                table: "email_log",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_email_log_fecha",
                table: "email_log",
                column: "fecha",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_email_log_tipo",
                table: "email_log",
                column: "tipo");

            migrationBuilder.CreateIndex(
                name: "IX_log_fecha",
                table: "log_sistema",
                column: "fecha_hora",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_log_nivel",
                table: "log_sistema",
                column: "nivel");

            migrationBuilder.CreateIndex(
                name: "IX_log_usuario",
                table: "log_sistema",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "UX_permiso_codigo",
                table: "permiso",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_personal_correoCorp",
                table: "personal",
                column: "correo_corporativo");

            migrationBuilder.CreateIndex(
                name: "IX_personal_documento",
                table: "personal",
                column: "documento");

            migrationBuilder.CreateIndex(
                name: "IX_personal_estado",
                table: "personal",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_reporte_generado",
                table: "reporte",
                columns: new[] { "generado_por", "fecha_generacion" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_reporte_tipo_formato",
                table: "reporte",
                columns: new[] { "tipo_reporte", "formato" });

            migrationBuilder.CreateIndex(
                name: "IX_repdet_solicitud",
                table: "reporte_detalle",
                column: "id_solicitud");

            migrationBuilder.CreateIndex(
                name: "IX_rol_permiso_perm",
                table: "rol_permiso",
                column: "id_permiso");

            migrationBuilder.CreateIndex(
                name: "IX_rol_registro_activo",
                table: "rol_registro",
                column: "es_activo");

            migrationBuilder.CreateIndex(
                name: "IX_rol_registro_bloque",
                table: "rol_registro",
                column: "bloque_tech");

            migrationBuilder.CreateIndex(
                name: "UX_rol_registro_nombre",
                table: "rol_registro",
                column: "nombre_rol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_sistema_activo",
                table: "roles_sistema",
                column: "es_activo");

            migrationBuilder.CreateIndex(
                name: "UX_roles_sistema_codigo",
                table: "roles_sistema",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_solicitud_creado_por",
                table: "solicitud",
                column: "creado_por");

            migrationBuilder.CreateIndex(
                name: "IX_solicitud_estado",
                table: "solicitud",
                column: "estado_solicitud");

            migrationBuilder.CreateIndex(
                name: "IX_solicitud_fechas",
                table: "solicitud",
                columns: new[] { "fecha_solicitud", "fecha_ingreso" });

            migrationBuilder.CreateIndex(
                name: "IX_solicitud_personal",
                table: "solicitud",
                column: "id_personal");

            migrationBuilder.CreateIndex(
                name: "IX_solicitud_rol_registro",
                table: "solicitud",
                column: "id_rol_registro");

            migrationBuilder.CreateIndex(
                name: "IX_solicitud_sla",
                table: "solicitud",
                column: "id_sla");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_estado",
                table: "usuario",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_id_personal",
                table: "usuario",
                column: "id_personal",
                unique: true,
                filter: "[id_personal] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_personal",
                table: "usuario",
                column: "id_personal");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_rol",
                table: "usuario",
                column: "id_rol_sistema");

            migrationBuilder.CreateIndex(
                name: "UX_usuario_username",
                table: "usuario",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerta");

            migrationBuilder.DropTable(
                name: "email_config");

            migrationBuilder.DropTable(
                name: "email_log");

            migrationBuilder.DropTable(
                name: "log_sistema");

            migrationBuilder.DropTable(
                name: "reporte_detalle");

            migrationBuilder.DropTable(
                name: "rol_permiso");

            migrationBuilder.DropTable(
                name: "reporte");

            migrationBuilder.DropTable(
                name: "solicitud");

            migrationBuilder.DropTable(
                name: "permiso");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "rol_registro");

            migrationBuilder.DropTable(
                name: "config_sla");

            migrationBuilder.DropTable(
                name: "personal");

            migrationBuilder.DropTable(
                name: "roles_sistema");
        }
    }
}
