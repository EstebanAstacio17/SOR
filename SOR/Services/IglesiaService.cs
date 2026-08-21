using System;
using System.Collections.Generic;
using SOR.Models;
using SOR.Repositories;

namespace SOR.Services
{
    public class IglesiaService
    {
        private readonly IglesiaRepository _iglesiaRepository;

        public IglesiaService()
        {
            _iglesiaRepository = new IglesiaRepository();
        }

        public List<Iglesia> ObtenerIglesias()
        {
            return _iglesiaRepository.ObtenerIglesias();
        }

        public int RegistrarIglesia(Iglesia modelo, int idUsuarioCreacion)
        {
            // Validaciones de negocio antes de guardar
            if (string.IsNullOrWhiteSpace(modelo.NombreIglesia))
            {
                throw new ArgumentException("El nombre de la iglesia es obligatorio.");
            }

            if (modelo.IdEquipo <= 0)
            {
                throw new ArgumentException("Debe asignar la iglesia a un equipo OCC válido.");
            }

            return _iglesiaRepository.RegistrarIglesia(modelo, idUsuarioCreacion);
        }

        public Iglesia ObtenerExpedienteIglesia(int idIglesia)
        {
            if (idIglesia <= 0) return null;
            return _iglesiaRepository.ObtenerExpedienteIglesia(idIglesia);
        }

        public void EvaluarParticipacion(int idParticipacion, bool participara, string justificacion, string estadoEvaluacion, int idEvaluador)
        {
            // Regla de Negocio: Si no participará, exige justificación obligatoria
            if (!participara && string.IsNullOrWhiteSpace(justificacion))
            {
                throw new ArgumentException("Debe proporcionar un motivo o justificación en caso de marcar que la iglesia NO participará esta temporada.");
            }

            _iglesiaRepository.EvaluarParticipacion(idParticipacion, participara, justificacion, estadoEvaluacion, idEvaluador);
        }

        public void DespacharRecursos(AsignacionRecursos modelo, int idDespachador)
        {
            if (modelo == null || modelo.IdParticipacion <= 0)
            {
                throw new ArgumentException("Modelo de asignación de recursos inválido.");
            }

            _iglesiaRepository.DespacharRecursos(modelo, idDespachador);
        }

        public void AgregarComentario(int idIglesia, int idUsuario, string comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
            {
                throw new ArgumentException("El contenido del comentario no puede estar vacío.");
            }

            _iglesiaRepository.AgregarComentario(idIglesia, idUsuario, comentario);
        }
    }
}
