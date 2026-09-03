using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using SOR.Models;
using SOR.Models.Reportes;

namespace SOR.DAL
{
    public class ReportesRepository : Repositories.BaseRepository
    {
        public void AsegurarEsquema()
        {
            try
            {
                using (var conn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    conn.Open();

                    string ddl = @"
                    -- 1. TABLA EOS: IGLESIAS PLANTADAS
                    IF OBJECT_ID('dbo.EOS_IglesiasPlantadas', 'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.EOS_IglesiasPlantadas (
                            IdIglesiaPlantada INT IDENTITY(1,1) PRIMARY KEY,
                            IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
                            IdEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
                            NombreIglesia NVARCHAR(150) NOT NULL,
                            PastorPrincipal NVARCHAR(150) NOT NULL,
                            Ubicacion NVARCHAR(200) NOT NULL,
                            CajitasEntregadas INT NOT NULL DEFAULT 0,
                            InscritosLGA INT NOT NULL DEFAULT 0,
                            FechaPlantacion DATE NULL,
                            Notas NVARCHAR(255) NULL,
                            FechaCreacion DATETIME DEFAULT GETDATE()
                        );
                    END;

                    -- 2. TABLA EOS: GRUPOS NO ALCANZADOS (GNA)
                    IF OBJECT_ID('dbo.EOS_GruposNoAlcanzados', 'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.EOS_GruposNoAlcanzados (
                            IdGNA INT IDENTITY(1,1) PRIMARY KEY,
                            IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
                            IdEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
                            NombreGNA NVARCHAR(150) NOT NULL,
                            CompaneroMinisterio NVARCHAR(150) NOT NULL,
                            CajitasEntregadas INT NOT NULL DEFAULT 0,
                            InscritosLGA INT NOT NULL DEFAULT 0,
                            NinosCreenJesus INT NOT NULL DEFAULT 0,
                            NinosOranComparten INT NOT NULL DEFAULT 0,
                            NinosGraduados INT NOT NULL DEFAULT 0,
                            Notas NVARCHAR(255) NULL,
                            FechaCreacion DATETIME DEFAULT GETDATE()
                        );
                    END;";

                    using (var cmd = new SqlCommand(ddl, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                // Continuity ensured
            }
        }

        public HashSet<int> ObtenerEquiposPermitidosJerarquico(Usuario u)
        {
            HashSet<int> set = new HashSet<int>();
            if (u == null) return set;

            bool esAdmin = u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2;
            if (esAdmin)
            {
                return null; // Acceso total
            }

            if (u.IdEquipo.HasValue && u.IdEquipo.Value > 0)
            {
                set.Add(u.IdEquipo.Value);
                ObtenerEquiposHijosRecursivo(u.IdEquipo.Value, set);
            }
            return set;
        }

        private void ObtenerEquiposHijosRecursivo(int idEquipoPadre, HashSet<int> set)
        {
            try
            {
                using (var conn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    conn.Open();
                    string sql = "SELECT IdEquipo FROM dbo.Equipos WHERE IdEquipoPadre = @Id;";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", idEquipoPadre);
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                int hId = Convert.ToInt32(dr["IdEquipo"]);
                                if (!set.Contains(hId))
                                {
                                    set.Add(hId);
                                    ObtenerEquiposHijosRecursivo(hId, set);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public List<SelectListItem> ObtenerListaTemporadas()
        {
            var list = new List<SelectListItem>();
            try
            {
                using (var conn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    conn.Open();
                    string sql = "SELECT IdTemporada, NombreTemporada, Activa FROM dbo.Temporadas ORDER BY Activa DESC, IdTemporada DESC;";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            bool activa = Convert.ToBoolean(dr["Activa"]);
                            list.Add(new SelectListItem
                            {
                                Value = dr["IdTemporada"].ToString(),
                                Text = dr["NombreTemporada"].ToString() + (activa ? " (Activa)" : "")
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public List<SelectListItem> ObtenerListaEquipos(HashSet<int> equiposPermitidos = null)
        {
            var list = new List<SelectListItem>();
            try
            {
                using (var conn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    conn.Open();
                    string sql = "SELECT IdEquipo, NombreEquipo FROM dbo.Equipos ORDER BY NombreEquipo;";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int idEq = Convert.ToInt32(dr["IdEquipo"]);
                            if (equiposPermitidos == null || equiposPermitidos.Contains(idEq))
                            {
                                list.Add(new SelectListItem
                                {
                                    Value = idEq.ToString(),
                                    Text = dr["NombreEquipo"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public List<SelectListItem> ObtenerListaIglesias(int? equipoId, HashSet<int> equiposPermitidos = null)
        {
            var list = new List<SelectListItem>();
            try
            {
                using (var conn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    conn.Open();
                    string sql = "SELECT IdIglesia, NombreIglesia, IdEquipo FROM dbo.Iglesias WHERE 1=1";
                    if (equipoId.HasValue && equipoId.Value > 0)
                    {
                        sql += " AND IdEquipo = @IdEquipo";
                    }
                    sql += " ORDER BY NombreIglesia;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (equipoId.HasValue && equipoId.Value > 0)
                        {
                            cmd.Parameters.AddWithValue("@IdEquipo", equipoId.Value);
                        }
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                int idEq = Convert.ToInt32(dr["IdEquipo"]);
                                if (equiposPermitidos == null || equiposPermitidos.Contains(idEq))
                                {
                                    list.Add(new SelectListItem
                                    {
                                        Value = dr["IdIglesia"].ToString(),
                                        Text = dr["NombreIglesia"].ToString()
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        // 1. REPORTE DE MOVILIZACIÓN
        public MovilizacionReporteDTO ObtenerReporteMovilizacion(int temporadaId, int? equipoId, int? iglesiaId)
        {
            var dto = new MovilizacionReporteDTO();
            try
            {
                using (var conn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    conn.Open();

                    string sqlMetricas = @"
                    SELECT 
                        -- Presentaciones de la Visión
                        ISNULL((SELECT COUNT(1) FROM dbo.Eventos e 
                                WHERE e.IdTemporada = @TemporadaId 
                                  AND (@EquipoId IS NULL OR e.IdEquipoCreador = @EquipoId)
                                  AND (e.TipoEvento = 'PresentacionVision' OR e.NombreEvento LIKE '%Vision%')), 0) AS TotalPresentacionesVision,

                        ISNULL((SELECT SUM(ISNULL(e.CantidadAsistentes, 0)) FROM dbo.Eventos e 
                                WHERE e.IdTemporada = @TemporadaId 
                                  AND (@EquipoId IS NULL OR e.IdEquipoCreador = @EquipoId)
                                  AND (e.TipoEvento = 'PresentacionVision' OR e.NombreEvento LIKE '%Vision%')), 0) AS TotalAsistentesVision,

                        -- Equipos Ministeriales capacitados
                        ISNULL((SELECT COUNT(DISTINCT pi.IdIglesia) FROM dbo.ParticipacionesIglesia pi
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)
                                  AND pi.Participara = 1), 0) AS EquiposMinisterialesCapacitados,

                        -- Cajitas entregadas a Compañeros de Ministerio
                        ISNULL((SELECT SUM(ISNULL(ar.OportunidadesEvangelisticas, 0)) FROM dbo.AsignacionesRecursos ar
                                INNER JOIN dbo.ParticipacionesIglesia pi ON ar.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS CajitasEntregadasCompaneros,

                        -- Eventos Evangelísticos y Niños asistentes
                        ISNULL((SELECT COUNT(1) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND re.TipoReporte = 'Evangelistico'
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS EventosEvangelisticos,

                        ISNULL((SELECT SUM(ISNULL(re.CantidadNinos, 0)) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND re.TipoReporte = 'Evangelistico'
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS NinosAsistentesEvangelisticos;";

                    using (var cmd = new SqlCommand(sqlMetricas, conn))
                    {
                        cmd.Parameters.AddWithValue("@TemporadaId", temporadaId);
                        cmd.Parameters.AddWithValue("@EquipoId", (object)equipoId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IglesiaId", (object)iglesiaId ?? DBNull.Value);

                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                dto.TotalPresentacionesVision = Convert.ToInt32(dr["TotalPresentacionesVision"]);
                                dto.TotalAsistentesVision = Convert.ToInt32(dr["TotalAsistentesVision"]);
                                dto.EquiposMinisterialesCapacitados = Convert.ToInt32(dr["EquiposMinisterialesCapacitados"]);
                                dto.CajitasEntregadasCompaneros = Convert.ToInt32(dr["CajitasEntregadasCompaneros"]);
                                dto.EventosEvangelisticos = Convert.ToInt32(dr["EventosEvangelisticos"]);
                                dto.NinosAsistentesEvangelisticos = Convert.ToInt32(dr["NinosAsistentesEvangelisticos"]);
                            }
                        }
                    }

                    // 2. Iglesias Plantadas
                    string sqlIp = @"
                    SELECT 
                        ip.IdIglesiaPlantada,
                        ip.IdTemporada,
                        ip.IdEquipo,
                        eq.NombreEquipo,
                        ip.NombreIglesia,
                        ip.PastorPrincipal,
                        ip.Ubicacion,
                        ip.CajitasEntregadas,
                        ip.InscritosLGA,
                        ip.FechaPlantacion,
                        ip.Notas
                    FROM dbo.EOS_IglesiasPlantadas ip
                    INNER JOIN dbo.Equipos eq ON ip.IdEquipo = eq.IdEquipo
                    WHERE ip.IdTemporada = @TemporadaId
                      AND (@EquipoId IS NULL OR ip.IdEquipo = @EquipoId)
                    ORDER BY ip.FechaPlantacion DESC, ip.IdIglesiaPlantada DESC;";

                    using (var cmd = new SqlCommand(sqlIp, conn))
                    {
                        cmd.Parameters.AddWithValue("@TemporadaId", temporadaId);
                        cmd.Parameters.AddWithValue("@EquipoId", (object)equipoId ?? DBNull.Value);

                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                dto.IglesiasPlantadas.Add(new IglesiaPlantadaDTO
                                {
                                    IdIglesiaPlantada = Convert.ToInt32(dr["IdIglesiaPlantada"]),
                                    IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                    IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                                    NombreEquipo = dr["NombreEquipo"].ToString(),
                                    NombreIglesia = dr["NombreIglesia"].ToString(),
                                    PastorPrincipal = dr["PastorPrincipal"].ToString(),
                                    Ubicacion = dr["Ubicacion"].ToString(),
                                    CajitasEntregadas = Convert.ToInt32(dr["CajitasEntregadas"]),
                                    InscritosLGA = Convert.ToInt32(dr["InscritosLGA"]),
                                    FechaPlantacion = dr["FechaPlantacion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaPlantacion"]) : null,
                                    Notas = dr["Notas"] != DBNull.Value ? dr["Notas"].ToString() : ""
                                });
                            }
                        }
                    }

                    // 3. GNA
                    string sqlGna = @"
                    SELECT 
                        g.IdGNA,
                        g.IdTemporada,
                        g.IdEquipo,
                        eq.NombreEquipo,
                        g.NombreGNA,
                        g.CompaneroMinisterio,
                        g.CajitasEntregadas,
                        g.InscritosLGA,
                        g.NinosCreenJesus,
                        g.NinosOranComparten,
                        g.NinosGraduados,
                        g.Notas
                    FROM dbo.EOS_GruposNoAlcanzados g
                    INNER JOIN dbo.Equipos eq ON g.IdEquipo = eq.IdEquipo
                    WHERE g.IdTemporada = @TemporadaId
                      AND (@EquipoId IS NULL OR g.IdEquipo = @EquipoId)
                    ORDER BY g.IdGNA DESC;";

                    using (var cmd = new SqlCommand(sqlGna, conn))
                    {
                        cmd.Parameters.AddWithValue("@TemporadaId", temporadaId);
                        cmd.Parameters.AddWithValue("@EquipoId", (object)equipoId ?? DBNull.Value);

                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                dto.GruposNoAlcanzados.Add(new GnaDTO
                                {
                                    IdGNA = Convert.ToInt32(dr["IdGNA"]),
                                    IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                    IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                                    NombreEquipo = dr["NombreEquipo"].ToString(),
                                    NombreGNA = dr["NombreGNA"].ToString(),
                                    CompaneroMinisterio = dr["CompaneroMinisterio"].ToString(),
                                    CajitasEntregadas = Convert.ToInt32(dr["CajitasEntregadas"]),
                                    InscritosLGA = Convert.ToInt32(dr["InscritosLGA"]),
                                    NinosCreenJesus = Convert.ToInt32(dr["NinosCreenJesus"]),
                                    NinosOranComparten = Convert.ToInt32(dr["NinosOranComparten"]),
                                    NinosGraduados = Convert.ToInt32(dr["NinosGraduados"]),
                                    Notas = dr["Notas"] != DBNull.Value ? dr["Notas"].ToString() : ""
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return dto;
        }

        // 2. REPORTE DE DISCIPULADO
        public DiscipuladoReporteDTO ObtenerReporteDiscipulado(int temporadaId, int? equipoId, int? iglesiaId)
        {
            var dto = new DiscipuladoReporteDTO();
            try
            {
                using (var conn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    conn.Open();
                    string sql = @"
                    SELECT 
                        -- Capacitaciones OCC
                        ISNULL((SELECT COUNT(1) FROM dbo.Eventos e 
                                WHERE e.IdTemporada = @TemporadaId 
                                  AND (@EquipoId IS NULL OR e.IdEquipoCreador = @EquipoId)
                                  AND (e.TipoEvento = 'Capacitacion' OR e.NombreEvento LIKE '%Capacitacion%' OR e.NombreEvento LIKE '%Taller%')), 0) AS TotalCapacitacionesOCC,

                        ISNULL((SELECT SUM(ISNULL(e.CantidadAsistentes, 0)) FROM dbo.Eventos e 
                                WHERE e.IdTemporada = @TemporadaId 
                                  AND (@EquipoId IS NULL OR e.IdEquipoCreador = @EquipoId)
                                  AND (e.TipoEvento = 'Capacitacion' OR e.NombreEvento LIKE '%Capacitacion%' OR e.NombreEvento LIKE '%Taller%')), 0) AS TotalAsistentesCapacitacion,

                        -- La Gran Aventura (LGA)
                        ISNULL((SELECT SUM(ISNULL(re.CantidadClases, 0)) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND re.TipoReporte = 'GranAventura'
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS LgaCursosImpartidos,

                        ISNULL((SELECT SUM(ISNULL(re.CantidadNinos, 0)) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND re.TipoReporte = 'GranAventura'
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS LgaNinosAsistentes,

                        ISNULL((SELECT SUM(ISNULL(re.CuantosAceptaronSenor, 0)) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND re.TipoReporte = 'GranAventura'
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS LgaDecisionesJesus,

                        ISNULL((SELECT SUM(ISNULL(re.CuantosComprometieron, 0)) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND re.TipoReporte = 'GranAventura'
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS LgaComprometidosOrarCompartir,

                        ISNULL((SELECT SUM(ISNULL(re.CuantosGraduaron, 0)) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND re.TipoReporte = 'GranAventura'
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS LgaGraduadosTotales,

                        -- Valores de Crecimiento (VDC / DET)
                        ISNULL((SELECT SUM(CASE WHEN ISNULL(re.CantidadNinos, 0) > 0 THEN re.CantidadNinos ELSE 0 END) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND (re.TipoReporte LIKE '%VDC%' OR re.TipoReporte = 'ValoresCrecimiento' OR re.Notas LIKE '%VDC%')
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS VdcAsistieronUnaClase,

                        ISNULL((SELECT SUM(ISNULL(re.CuantosGraduaron, 0)) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND (re.TipoReporte LIKE '%VDC%' OR re.TipoReporte = 'ValoresCrecimiento' OR re.Notas LIKE '%VDC%')
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS VdcAsistieronSeisClases,

                        ISNULL((SELECT SUM(ISNULL(re.CuantosComprometieron, 0)) FROM dbo.ReportesEventos re
                                INNER JOIN dbo.ParticipacionesIglesia pi ON re.IdParticipacion = pi.IdParticipacion
                                INNER JOIN dbo.Iglesias i ON pi.IdIglesia = i.IdIglesia
                                WHERE pi.IdTemporada = @TemporadaId
                                  AND (re.TipoReporte LIKE '%VDC%' OR re.TipoReporte = 'ValoresCrecimiento' OR re.Notas LIKE '%VDC%')
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS VdcContinuaronLgaODet;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TemporadaId", temporadaId);
                        cmd.Parameters.AddWithValue("@EquipoId", (object)equipoId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IglesiaId", (object)iglesiaId ?? DBNull.Value);

                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                dto.TotalCapacitacionesOCC = Convert.ToInt32(dr["TotalCapacitacionesOCC"]);
                                dto.TotalAsistentesCapacitacion = Convert.ToInt32(dr["TotalAsistentesCapacitacion"]);
                                dto.LgaCursosImpartidos = Convert.ToInt32(dr["LgaCursosImpartidos"]);
                                dto.LgaNinosAsistentes = Convert.ToInt32(dr["LgaNinosAsistentes"]);
                                dto.LgaDecisionesJesus = Convert.ToInt32(dr["LgaDecisionesJesus"]);
                                dto.LgaComprometidosOrarCompartir = Convert.ToInt32(dr["LgaComprometidosOrarCompartir"]);
                                dto.LgaGraduadosTotales = Convert.ToInt32(dr["LgaGraduadosTotales"]);
                                dto.VdcAsistieronUnaClase = Convert.ToInt32(dr["VdcAsistieronUnaClase"]);
                                dto.VdcAsistieronSeisClases = Convert.ToInt32(dr["VdcAsistieronSeisClases"]);
                                dto.VdcContinuaronLgaODet = Convert.ToInt32(dr["VdcContinuaronLgaODet"]);
                            }
                        }
                    }
                }
            }
            catch { }
            return dto;
        }

        // 3. REPORTE DE ORACIÓN
        public OracionReporteDTO ObtenerReporteOracion(int temporadaId, int? equipoId, int? iglesiaId)
        {
            var dto = new OracionReporteDTO();
            try
            {
                using (var conn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    conn.Open();
                    string sql = @"
                    SELECT 
                        ISNULL((SELECT COUNT(1) FROM dbo.Eventos e 
                                WHERE e.IdTemporada = @TemporadaId 
                                  AND (@EquipoId IS NULL OR e.IdEquipoCreador = @EquipoId)
                                  AND (e.TipoEvento = 'Oracion' OR e.NombreEvento LIKE '%Oracion%')), 0) AS EventosOracionOrganizados,

                        ISNULL((SELECT SUM(ISNULL(e.CantidadAsistentes, 0)) FROM dbo.Eventos e 
                                WHERE e.IdTemporada = @TemporadaId 
                                  AND (@EquipoId IS NULL OR e.IdEquipoCreador = @EquipoId)
                                  AND (e.TipoEvento = 'Oracion' OR e.NombreEvento LIKE '%Oracion%')), 0) AS TotalAsistentesOracion,

                        ISNULL((SELECT COUNT(1) FROM dbo.CompanerosOracion co
                                INNER JOIN dbo.Iglesias i ON co.IdIglesia = i.IdIglesia
                                WHERE co.IdTemporada = @TemporadaId
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS CompanerosOracionReportados,

                        ISNULL((SELECT COUNT(1) FROM dbo.CompanerosOracion co
                                INNER JOIN dbo.Iglesias i ON co.IdIglesia = i.IdIglesia
                                WHERE co.IdTemporada = @TemporadaId
                                  AND (co.Telefono IS NOT NULL AND LEN(co.Telefono) > 3)
                                  AND (@EquipoId IS NULL OR i.IdEquipo = @EquipoId)
                                  AND (@IglesiaId IS NULL OR i.IdIglesia = @IglesiaId)), 0) AS MiembrosRedOracionLocal;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TemporadaId", temporadaId);
                        cmd.Parameters.AddWithValue("@EquipoId", (object)equipoId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IglesiaId", (object)iglesiaId ?? DBNull.Value);

                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                dto.EventosOracionOrganizados = Convert.ToInt32(dr["EventosOracionOrganizados"]);
                                dto.TotalAsistentesOracion = Convert.ToInt32(dr["TotalAsistentesOracion"]);
                                dto.CompanerosOracionReportados = Convert.ToInt32(dr["CompanerosOracionReportados"]);
                                dto.MiembrosRedOracionLocal = Convert.ToInt32(dr["MiembrosRedOracionLocal"]);
                            }
                        }
                    }
                }
            }
            catch { }
            return dto;
        }

        public bool GuardarIglesiaPlantada(IglesiaPlantadaDTO dto)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = @"
                IF @Id = 0
                BEGIN
                    INSERT INTO dbo.EOS_IglesiasPlantadas 
                        (IdTemporada, IdEquipo, NombreIglesia, PastorPrincipal, Ubicacion, CajitasEntregadas, InscritosLGA, FechaPlantacion, Notas)
                    VALUES 
                        (@TemporadaId, @EquipoId, @NombreIglesia, @Pastor, @Ubicacion, @Cajitas, @LGA, @Fecha, @Notas);
                END
                ELSE
                BEGIN
                    UPDATE dbo.EOS_IglesiasPlantadas
                    SET IdEquipo = @EquipoId,
                        NombreIglesia = @NombreIglesia,
                        PastorPrincipal = @Pastor,
                        Ubicacion = @Ubicacion,
                        CajitasEntregadas = @Cajitas,
                        InscritosLGA = @LGA,
                        FechaPlantacion = @Fecha,
                        Notas = @Notas
                    WHERE IdIglesiaPlantada = @Id;
                END";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", dto.IdIglesiaPlantada);
                    cmd.Parameters.AddWithValue("@TemporadaId", dto.IdTemporada);
                    cmd.Parameters.AddWithValue("@EquipoId", dto.IdEquipo);
                    cmd.Parameters.AddWithValue("@NombreIglesia", dto.NombreIglesia);
                    cmd.Parameters.AddWithValue("@Pastor", dto.PastorPrincipal);
                    cmd.Parameters.AddWithValue("@Ubicacion", dto.Ubicacion);
                    cmd.Parameters.AddWithValue("@Cajitas", dto.CajitasEntregadas);
                    cmd.Parameters.AddWithValue("@LGA", dto.InscritosLGA);
                    cmd.Parameters.AddWithValue("@Fecha", (object)dto.FechaPlantacion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notas", (object)dto.Notas ?? DBNull.Value);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool EliminarIglesiaPlantada(int id)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                using (var cmd = new SqlCommand("DELETE FROM dbo.EOS_IglesiasPlantadas WHERE IdIglesiaPlantada = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool GuardarGNA(GnaDTO dto)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = @"
                IF @Id = 0
                BEGIN
                    INSERT INTO dbo.EOS_GruposNoAlcanzados 
                        (IdTemporada, IdEquipo, NombreGNA, CompaneroMinisterio, CajitasEntregadas, InscritosLGA, NinosCreenJesus, NinosOranComparten, NinosGraduados, Notas)
                    VALUES 
                        (@TemporadaId, @EquipoId, @NombreGNA, @Companero, @Cajitas, @LGA, @Creen, @Oran, @Graduados, @Notas);
                END
                ELSE
                BEGIN
                    UPDATE dbo.EOS_GruposNoAlcanzados
                    SET IdEquipo = @EquipoId,
                        NombreGNA = @NombreGNA,
                        CompaneroMinisterio = @Companero,
                        CajitasEntregadas = @Cajitas,
                        InscritosLGA = @LGA,
                        NinosCreenJesus = @Creen,
                        NinosOranComparten = @Oran,
                        NinosGraduados = @Graduados,
                        Notas = @Notas
                    WHERE IdGNA = @Id;
                END";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", dto.IdGNA);
                    cmd.Parameters.AddWithValue("@TemporadaId", dto.IdTemporada);
                    cmd.Parameters.AddWithValue("@EquipoId", dto.IdEquipo);
                    cmd.Parameters.AddWithValue("@NombreGNA", dto.NombreGNA);
                    cmd.Parameters.AddWithValue("@Companero", dto.CompaneroMinisterio);
                    cmd.Parameters.AddWithValue("@Cajitas", dto.CajitasEntregadas);
                    cmd.Parameters.AddWithValue("@LGA", dto.InscritosLGA);
                    cmd.Parameters.AddWithValue("@Creen", dto.NinosCreenJesus);
                    cmd.Parameters.AddWithValue("@Oran", dto.NinosOranComparten);
                    cmd.Parameters.AddWithValue("@Graduados", dto.NinosGraduados);
                    cmd.Parameters.AddWithValue("@Notas", (object)dto.Notas ?? DBNull.Value);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool EliminarGNA(int id)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                using (var cmd = new SqlCommand("DELETE FROM dbo.EOS_GruposNoAlcanzados WHERE IdGNA = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
