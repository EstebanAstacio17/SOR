using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// Requiere instalar vía NuGet: Google.Cloud.Firestore
// using Google.Cloud.Firestore;

namespace SOR.Helpers
{
    public class FirestoreHelper
    {
        private static string ObtenerProjectId()
        {
            return Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID") 
                ?? System.Configuration.ConfigurationManager.AppSettings["FirebaseProjectId"] 
                ?? "sor-occ-prod";
        }

        /*
        // Ejemplo de inicialización y guardado en Firestore en C#:
        private readonly FirestoreDb _db;

        public FirestoreHelper()
        {
            // La ruta a las credenciales se puede definir mediante GOOGLE_APPLICATION_CREDENTIALS
            _db = FirestoreDb.Create(ObtenerProjectId());
        }

        public async Task<string> GuardarUsuarioAsync(string correo, string nombres, int idRol)
        {
            CollectionReference collection = _db.Collection("usuarios");
            var datos = new Dictionary<string, object>
            {
                { "correo", correo },
                { "nombres", nombres },
                { "idRol", idRol },
                { "fechaRegistro", Timestamp.GetCurrentTimestamp() }
            };

            DocumentReference docRef = await collection.AddAsync(datos);
            return docRef.Id;
        }

        public async Task<Dictionary<string, object>> ObtenerUsuarioPorCorreoAsync(string correo)
        {
            Query query = _db.Collection("usuarios").WhereEqualTo("correo", correo).Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count > 0)
            {
                return snapshot.Documents[0].ToDictionary();
            }
            return null;
        }
        */
    }
}
