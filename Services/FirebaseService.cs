using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

namespace API_DigiBook.Services
{
    public class FirebaseService
    {
        private static FirestoreDb? _firestoreDb;
        private static readonly object _lock = new object();

        public static FirestoreDb GetFirestoreDb()
        {
            if (_firestoreDb == null)
            {
                lock (_lock)
                {
                    if (_firestoreDb == null)
                    {
                        var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
                        var credentialPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH");
                        
                        if (string.IsNullOrEmpty(projectId))
                        {
                            throw new InvalidOperationException(
                                "FIREBASE_PROJECT_ID environment variable is not set. Please check your .env file."
                            );
                        }

                        if (string.IsNullOrEmpty(credentialPath))
                        {
                            throw new InvalidOperationException(
                                "FIREBASE_CREDENTIAL_PATH environment variable is not set. Please check your .env file."
                            );
                        }

                        if (!File.Exists(credentialPath))
                        {
                            throw new FileNotFoundException(
                                $"Firebase credential file not found at: {credentialPath}"
                            );
                        }

                        // Set GOOGLE_APPLICATION_CREDENTIALS environment variable for Firestore
                        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);

                        _firestoreDb = FirestoreDb.Create(projectId);
                    }
                }
            }
            return _firestoreDb;
        }

        public static void InitializeFirebase()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var credentialPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH");
                
                if (string.IsNullOrEmpty(credentialPath))
                {
                    throw new InvalidOperationException(
                        "FIREBASE_CREDENTIAL_PATH environment variable is not set. Please check your .env file."
                    );
                }

                if (!File.Exists(credentialPath))
                {
                    throw new FileNotFoundException(
                        $"Firebase credential file not found at: {credentialPath}"
                    );
                }

                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(credentialPath)
                });

                Console.WriteLine("✓ Firebase initialized successfully");
            }
        }
    }
}
