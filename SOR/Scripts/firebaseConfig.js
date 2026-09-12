// Configuración e Inicialización de Firebase SDK para el Cliente Web
// Se recomienda inyectar estas variables desde el backend o variables de entorno

const firebaseConfig = {
  apiKey: window.__ENV__?.FIREBASE_API_KEY || "AIzaSy_REPLACE_WITH_YOUR_KEY",
  authDomain: window.__ENV__?.FIREBASE_AUTH_DOMAIN || "sor-occ-prod.firebaseapp.com",
  projectId: window.__ENV__?.FIREBASE_PROJECT_ID || "sor-occ-prod",
  storageBucket: window.__ENV__?.FIREBASE_STORAGE_BUCKET || "sor-occ-prod.appspot.com",
  messagingSenderId: window.__ENV__?.FIREBASE_MESSAGING_SENDER_ID || "123456789012",
  appId: window.__ENV__?.FIREBASE_APP_ID || "1:123456789012:web:abcdef123456"
};

// Inicializar Firebase (Modular SDK v9+)
// Importar: import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
// import { getFirestore, collection, addDoc, getDocs } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-firestore.js";

let app, db;
if (typeof firebase !== 'undefined') {
    app = firebase.initializeApp(firebaseConfig);
    db = firebase.firestore();
}

// Ejemplo de guardado de datos en Firestore (Lado Cliente):
async function registrarSolicitudVoluntario(datos) {
    try {
        const docRef = await db.collection("solicitudes_voluntarios").add({
            nombres: datos.nombres,
            apellidos: datos.apellidos,
            correo: datos.correo,
            telefono: datos.telefono,
            fechaCreacion: firebase.firestore.FieldValue.serverTimestamp()
        });
        console.log("Documento guardado con ID: ", docRef.id);
        return { success: true, id: docRef.id };
    } catch (e) {
        console.error("Error al registrar en Firestore: ", e);
        return { success: false, error: e };
    }
}
