using System;

namespace SOR.Models
{
    public class PerfilCoordinador
    {
        public int IdPerfil { get; set; }
        public int IdUsuario { get; set; }

        // Sección 1: Datos Personales
        public string PrimerNombre { get; set; }
        public string OtrosNombres { get; set; }
        public string PrimerApellido { get; set; }
        public string OtrosApellidos { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string Sector { get; set; }
        public string Ciudad { get; set; }
        public string Provincia { get; set; }
        public string Pais { get; set; } = "República Dominicana";
        public string Nacionalidad { get; set; } = "Dominicana";
        public string Talla { get; set; } // Talla de camisa / ropa
        public string NumeroDocumento { get; set; } // Cédula
        public string DocumentoAdjuntoRuta { get; set; }
        public string Sexo { get; set; } // Varón, Hembra, Otro
        public string EstadoCivil { get; set; } // Soltero, Casado, Divorciado, Viudo, Unión Libre, Otro
        public string NumeroPasaporte { get; set; }
        public bool NoPoseePasaporte { get; set; }
        public string PasaporteAdjuntoRuta { get; set; }
        public string TelefonoFijo { get; set; }
        public string TelefonoCelularWhatsApp { get; set; }
        public string Correo { get; set; } // Correo readonly
        public string FotoRuta { get; set; }

        // Datos de Cónyuge y Contacto de Emergencia
        public string DatosConyugue { get; set; }
        public string ContactoEmergencia { get; set; }

        // Sección 2: Datos Ministeriales, Laborales y Educativos Detallados
        public string IglesiaLocal { get; set; }
        public string PastorIglesiaLocal { get; set; }
        public string CargoIglesiaLocal { get; set; }
        public int? AniosServicioMinisterial { get; set; }
        public string InfoMinisterial { get; set; }

        public string NivelEducativo { get; set; } // Secundaria, Técnico, Licenciatura, Maestría, Doctorado
        public string ProfesionCarrera { get; set; }
        public string InfoEducativa { get; set; }

        public string OcupacionEmpresaLaboral { get; set; }
        public string TelefonoTrabajo { get; set; }
        public string InfoLaboral { get; set; }

        public string CapacitacionesOCC { get; set; }

        // Sección 3: Datos OCC

        public string Ministerio { get; set; }
        public int? IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public int? IdPosicion { get; set; }
        public string NombrePosicion { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public DateTime? FechaCompletado { get; set; }
    }

    public class Equipo
    {
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public int IdNivelEquipo { get; set; }
        public string NombreNivel { get; set; }
        public int RangoJerarquico { get; set; }
        public int? IdEquipoPadre { get; set; }
    }

    public class PosicionOCC
    {
        public int IdPosicion { get; set; }
        public string NombrePosicion { get; set; }
        public string Descripcion { get; set; }
        public bool Ocupado { get; set; }
    }

    public class SolicitudUsuarioViewModel
    {
        public int IdUsuario { get; set; }
        public string Correo { get; set; }
        public int IdRolSeguridad { get; set; }
        public string NombreRol { get; set; }
        public int IdEstado { get; set; }
        public string NombreEstado { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public PerfilCoordinador Perfil { get; set; }
    }

}
