using System;

namespace SOR.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Correo { get; set; }
        public string Clave { get; set; }
        public string ConfirmarClave { get; set; }

        // Rol de Seguridad (1: SuperAdmin, 2: Administrador, 3: Coordinador)
        public string PrimerNombre { get; set; }
        public string PrimerApellido { get; set; }
        public string NombreCompleto => $"{PrimerNombre} {PrimerApellido}".Trim();

        public int IdRolSeguridad { get; set; }
        public string NombreRol { get; set; }

        // Estado de Cuenta (1: PendienteCorreo, 2: CorreoAprobado, 3: PerfilPendiente, 4: Activo, 5: Rechazado, 6: Suspendido)
        public int IdEstado { get; set; }
        public string NombreEstado { get; set; }

        // Asignación Organizacional OCC
        public int? IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public string NombreNivel { get; set; } // ENL, ERLE, ERL
        public int? RangoJerarquico { get; set; } // 1: ENL, 2: ERLE, 3: ERL


        public int? IdPosicion { get; set; }
        public string NombrePosicion { get; set; }

        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }
    }
}