using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Aplicacion.Autorizacion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;
using Continuum.Identidad.Infraestructura.Memoria;

namespace Continuum.Identidad.Tests.Administracion;

internal sealed class AuditoriaAdministracionFalsa : IAuditoriaAdministracion
{
    public List<EventoAdministracion> Eventos { get; } = [];
    public bool Falla { get; set; }

    public Task RegistrarAsync(EventoAdministracion evento, CancellationToken ct = default)
    {
        if (Falla) throw new InvalidOperationException("Bitácora no disponible.");
        Eventos.Add(evento);
        return Task.CompletedTask;
    }
}

internal sealed class SedesFalsas : IConsultaSedes
{
    private readonly HashSet<(Guid Organizacion, Guid Sede)> _sedes = [];
    public void Agregar(Guid organizacionId, Guid sedeId) => _sedes.Add((organizacionId, sedeId));
    public Task<bool> ExisteEnOrganizacionAsync(Guid organizacionId, Guid sedeId, CancellationToken ct = default) =>
        Task.FromResult(_sedes.Contains((organizacionId, sedeId)));
}

internal sealed class CatalogoFalso : ICatalogoClinico
{
    private readonly HashSet<(Guid Organizacion, Guid Id)> _especialidades = [], _procedimientos = [];
    public void AgregarEspecialidad(Guid organizacionId, Guid id) => _especialidades.Add((organizacionId, id));
    public void AgregarProcedimiento(Guid organizacionId, Guid id) => _procedimientos.Add((organizacionId, id));

    public Task<bool> EspecialidadVigenteAsync(Guid organizacionId, Guid especialidadId, CancellationToken ct = default) =>
        Task.FromResult(_especialidades.Contains((organizacionId, especialidadId)));

    public Task<bool> ProcedimientosVigentesAsync(Guid organizacionId, IReadOnlyCollection<Guid> procedimientoIds, CancellationToken ct = default) =>
        Task.FromResult(procedimientoIds.All(p => _procedimientos.Contains((organizacionId, p))));
}

internal sealed class AvisosAdministracionFalsos : IAvisosAdministracion
{
    public List<(Guid OrganizacionId, IReadOnlyList<AvisoVencimiento> Avisos)> Enviados { get; } = [];
    public bool Falla { get; set; }

    public Task NotificarVencimientosAsync(Guid organizacionId, IReadOnlyList<AvisoVencimiento> avisos, CancellationToken ct = default)
    {
        if (Falla) throw new InvalidOperationException("Canal de avisos no disponible.");
        Enviados.Add((organizacionId, avisos));
        return Task.CompletedTask;
    }
}

/// <summary>República Dominicana: UTC−4 sin horario de verano (zona por defecto de la sede).</summary>
internal sealed class ZonaHorariaFalsa : IZonaHorariaSede
{
    public static readonly TimeZoneInfo SantoDomingo =
        TimeZoneInfo.CreateCustomTimeZone("America/Santo_Domingo", TimeSpan.FromHours(-4), "Santo Domingo", "Santo Domingo");

    public Task<TimeZoneInfo> ObtenerAsync(Guid sedeId, CancellationToken ct = default) => Task.FromResult(SantoDomingo);
}

/// <summary>
/// Arma el módulo de administración sobre repositorios en memoria, con una organización, dos sedes,
/// un administrador funcional ya sembrado y una organización ajena. Todos los datos son ficticios.
/// </summary>
internal sealed class EntornoAdministracion
{
    public static readonly Guid Org = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    public static readonly Guid OtraOrg = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    public static readonly Guid Sede = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    public static readonly Guid SedeNorte = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    public static readonly Guid SedeAjena = Guid.Parse("00000000-0000-0000-0000-0000000000b3");
    public static readonly Guid Cardiologia = Guid.Parse("00000000-0000-0000-0000-000000000051");
    public static readonly Guid Psicologia = Guid.Parse("00000000-0000-0000-0000-000000000052");
    public static readonly Guid Electrocardiograma = Guid.Parse("00000000-0000-0000-0000-000000000061");
    public static readonly Guid Ecocardiograma = Guid.Parse("00000000-0000-0000-0000-000000000062");

    public RelojFalso Reloj { get; } = new();
    public AlmacenMemoria Almacen { get; } = new();
    public RepositorioUsuariosMemoria Usuarios { get; }
    public RepositorioRolesAsignadosMemoria Roles { get; }
    public RepositorioProfesionalesMemoria Profesionales { get; }
    public RepositorioHabilitacionesMemoria Habilitaciones { get; }
    public UnidadDeTrabajoMemoria UnidadDeTrabajo { get; }
    public AuditoriaAdministracionFalsa Auditoria { get; } = new();
    public AuditoriaFalsa AuditoriaIdentidad { get; } = new();
    public SedesFalsas Sedes { get; } = new();
    public CatalogoFalso Catalogo { get; } = new();
    public AvisosAdministracionFalsos Avisos { get; } = new();
    public SesionesFalsas Sesiones { get; } = new();

    public ConsultaRolesPorSede ConsultaRoles { get; }
    public ConsultaHabilitaciones ConsultaHabilitaciones { get; }
    public ServicioUsuarios ServicioUsuarios { get; }
    public ServicioRoles ServicioRoles { get; }
    public ServicioHabilitaciones ServicioHabilitaciones { get; }
    public ServicioVencimientosHabilitaciones ServicioVencimientos { get; }

    /// <summary>Administrador funcional de <see cref="Org"/> con rol en <see cref="Sede"/>.</summary>
    public Usuario Administrador { get; }
    public ActorAdministracion Actor { get; }

    public EntornoAdministracion()
    {
        Usuarios = new(Almacen);
        Roles = new(Almacen);
        Profesionales = new(Almacen);
        Habilitaciones = new(Almacen);
        UnidadDeTrabajo = new(Almacen);

        Sedes.Agregar(Org, Sede);
        Sedes.Agregar(Org, SedeNorte);
        Sedes.Agregar(OtraOrg, SedeAjena);
        foreach (var org in new[] { Org, OtraOrg })
        {
            Catalogo.AgregarEspecialidad(org, Cardiologia);
            Catalogo.AgregarEspecialidad(org, Psicologia);
            Catalogo.AgregarProcedimiento(org, Electrocardiograma);
            Catalogo.AgregarProcedimiento(org, Ecocardiograma);
        }

        ConsultaRoles = new(Usuarios, Roles, Profesionales);
        ConsultaHabilitaciones = new(Habilitaciones);

        var politicaSesion = new PoliticaSesionFalsa();
        var sesiones = new ServicioSesiones(Sesiones, politicaSesion, Reloj, AuditoriaIdentidad);
        var guardia = new GuardiaAdministracion(ConsultaRoles, Auditoria, Reloj);

        ServicioUsuarios = new(guardia, Usuarios, Roles, new HasheadorFalso(), sesiones, UnidadDeTrabajo, Auditoria, Reloj);
        ServicioRoles = new(guardia, Usuarios, Roles, Sedes, UnidadDeTrabajo, Auditoria, Reloj);
        ServicioHabilitaciones = new(guardia, Usuarios, Profesionales, Habilitaciones, Catalogo,
            new ZonaHorariaFalsa(), UnidadDeTrabajo, Auditoria, Reloj);
        ServicioVencimientos = new(Habilitaciones, Avisos, UnidadDeTrabajo, Reloj);

        Administrador = SembrarUsuario("admin@clinica.test", Org);
        Roles.Sembrar(new RolAsignado(Administrador.Id, Rol.AdminFuncional, Sede));
        Actor = new ActorAdministracion(Administrador.Id, Sede);
    }

    public Usuario SembrarUsuario(string correo, Guid? organizacionId = null)
    {
        var usuario = Usuario.Crear(organizacionId ?? Org, correo, "hash");
        Usuarios.Sembrar(usuario);
        return usuario;
    }

    /// <summary>Un usuario con un único rol en <see cref="Sede"/> que actúa como administrador de pruebas negativas.</summary>
    public ActorAdministracion ActorConRol(Rol rol, string correo = "otro@clinica.test")
    {
        var u = SembrarUsuario(correo);
        Roles.Sembrar(new RolAsignado(u.Id, rol, Sede));
        return new ActorAdministracion(u.Id, Sede);
    }

    /// <summary>Registra un profesional de <see cref="Org"/> por el caso de uso y devuelve su identificador.</summary>
    public async Task<Guid> SembrarProfesionalAsync(string correo = "dra.perez@clinica.test")
    {
        var u = SembrarUsuario(correo);
        var r = await ServicioHabilitaciones.RegistrarProfesionalAsync(
            Actor, new(u.Id, "Ana", "Pérez", "Médico", Licencia: "LIC-0001"));
        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        Auditoria.Eventos.Clear();
        return r.Id!.Value;
    }

    public DateOnly HoyEnSede => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(Reloj.Ahora, ZonaHorariaFalsa.SantoDomingo).DateTime);
}
