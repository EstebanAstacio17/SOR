using SOR.Helpers;
using SOR.Models;
using SOR.Repositories;

namespace SOR.Services
{
    public class UsuarioService
    {
        private readonly UsuarioRepository _usuarioRepository;

        public UsuarioService()
        {
            _usuarioRepository = new UsuarioRepository();
        }

        /// <summary>
        /// Valida el correo y contraseña de un usuario.
        /// Si la contraseña es válida, retorna el objeto Usuario con todos sus detalles.
        /// </summary>
        public Usuario ValidarUsuario(string correo, string clavePlana)
        {
            Usuario usuario = _usuarioRepository.ObtenerUsuarioPorCorreo(correo);

            if (usuario == null)
            {
                return null;
            }

            // Verificar la clave con soporte de salt:hash y retrocompatibilidad
            if (Criptografia.VerificarClave(clavePlana, usuario.Clave))
            {
                // Limpiar la propiedad Clave para evitar exponer el hash en la sesión
                usuario.Clave = null;
                return usuario;
            }

            return null;
        }

        /// <summary>
        /// Registra un nuevo usuario aplicando cifrado robusto de contraseñas.
        /// </summary>
        public bool RegistrarUsuario(Usuario oUsuario, out string mensaje)
        {
            // Cifrar contraseña usando el esquema moderno de salt:hash
            string claveFormateada = Criptografia.CrearClaveFormateada(oUsuario.Clave);

            return _usuarioRepository.RegistrarUsuario(oUsuario.Correo, claveFormateada, out mensaje);
        }
    }
}
