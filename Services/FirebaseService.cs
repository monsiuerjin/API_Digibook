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
                        var credentialJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");
                        var credentialPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH");
                        
                        if (string.IsNullOrEmpty(projectId))
                        {
                            throw new InvalidOperationException(
                                "FIREBASE_PROJECT_ID environment variable is not set."
                            );
                        }

                        if (!string.IsNullOrEmpty(credentialJson))
                        {
                            // If JSON is provided, we don't need the path
                            // We use dummy path to satisfy FirestoreDb.Create if needed, 
                            // but actually we should use GoogleCredential
                            var credential = GoogleCredential.FromJson(credentialJson);
                            _firestoreDb = new FirestoreDbBuilder
                            {
                                ProjectId = projectId,
                                Credential = credential
                            }.Build();
                            Console.WriteLine("✓ Firestore initialized using JSON credentials");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(credentialPath))
                            {
                                throw new InvalidOperationException(
                                    "Neither FIREBASE_CREDENTIALS_JSON nor FIREBASE_CREDENTIAL_PATH is set."
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
                            Console.WriteLine("✓ Firestore initialized using file credentials");
                        }
                    }
                }
            }
            return _firestoreDb;
        }

        public static void InitializeFirebase()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var credentialJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");
                var credentialPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH");
                
                GoogleCredential credential;

                if (!string.IsNullOrEmpty(credentialJson))
                {
                    credential = GoogleCredential.FromJson(credentialJson);
                    Console.WriteLine("✓ Firebase initialized using JSON credentials");
                }
                else
                {
                    if (string.IsNullOrEmpty(credentialPath))
                    {
                        throw new InvalidOperationException(
                            "Neither FIREBASE_CREDENTIALS_JSON nor FIREBASE_CREDENTIAL_PATH is set."
                        );
                    }

                    if (!File.Exists(credentialPath))
                    {
                        throw new FileNotFoundException(
                            $"Firebase credential file not found at: {credentialPath}"
                        );
                    }
                    credential = GoogleCredential.FromFile(credentialPath);
                    Console.WriteLine("✓ Firebase initialized using file credentials");
                }

                FirebaseApp.Create(new AppOptions()
                {
                    Credential = credential
                });

                Console.WriteLine("✓ Firebase Admin SDK initialized successfully");
            }
        }
    }
}
