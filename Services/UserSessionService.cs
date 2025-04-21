using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AuthApi.Services
{
    public class UserSessionService
    {
        // Use ConcurrentDictionary for thread safety
        // Key: userId, Value: token + expires time
        private readonly ConcurrentDictionary<string, (string Token, DateTime ExpiresAt)> _activeSessions = new();

        // Check if any user is currently logged in
        public bool AnyUserLoggedIn()
        {
            CleanupExpiredSessions(); // First clean up expired sessions
            return _activeSessions.Count > 0;
        }

        // Get the current logged-in user's ID
        public string? GetCurrentUser()
        {
            // Clean up expired sessions first
            CleanupExpiredSessions();
            
            // If no users are logged in, return null
            if (!AnyUserLoggedIn())
                return null;
            
            // Get the first active session
            var activeSession = _activeSessions.FirstOrDefault();
            if (!string.IsNullOrEmpty(activeSession.Key))
            {
                return activeSession.Key;
            }
            
            return null;
        }

        // Check if user is already logged in
        public bool IsUserLoggedIn(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;
                
            // If the user has an active session, check if it's still valid
            if (_activeSessions.TryGetValue(userId, out var sessionInfo))
            {
                if (sessionInfo.ExpiresAt > DateTime.UtcNow)
                {
                    return true; // User is logged in with a valid session
                }
                else
                {
                    // Session expired, remove it
                    _activeSessions.TryRemove(userId, out _);
                    return false;
                }
            }
            
            return false; // No active session
        }
        
        // Add or update user session
        public void AddUserSession(string userId, string token, DateTime expiresAt)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return;
            
            // Clean up expired sessions first
            CleanupExpiredSessions();
            
            // Only allow one active session at a time
            if (AnyUserLoggedIn() && !_activeSessions.ContainsKey(userId))
            {
                Console.WriteLine("Attempted to add session but another user is already logged in");
                throw new InvalidOperationException("Cannot add new session: another user is already logged in");
            }
                
            _activeSessions.AddOrUpdate(userId, 
                _ => (token, expiresAt),  
                (_, existingValue) => (token, expiresAt));
                
            Console.WriteLine($"User session added/updated for user {userId}, expires at {expiresAt} UTC");
            Console.WriteLine($"Total active sessions: {_activeSessions.Count}");
        }
        
        // Logout the currently active user
        public bool LogoutCurrentUser()
        {
            // Clean up expired sessions first
            CleanupExpiredSessions();
            
            // If no users are logged in, return false
            if (!AnyUserLoggedIn())
                return false;
                
            // Get the first active session
            var activeSession = _activeSessions.FirstOrDefault();
            if (activeSession.Key != null)
            {
                Console.WriteLine($"Logging out current user: {activeSession.Key}");
                return RemoveUserSession(activeSession.Key);
            }
            
            return false;
        }
        
        // Remove user session
        public bool RemoveUserSession(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;
                
            if (!IsUserLoggedIn(userId))
            {
                Console.WriteLine($"Attempted to remove session for user {userId} but no active session found");
                return false; // User is not logged in
            }
            
            var result = _activeSessions.TryRemove(userId, out _);
            Console.WriteLine($"User session for {userId} removed successfully: {result}");
            Console.WriteLine($"Total active sessions after removal: {_activeSessions.Count}");
            return result;
        }
        
        // Get user token
        public string GetUserToken(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return string.Empty;
            
            if (_activeSessions.TryGetValue(userId, out var sessionInfo) && 
                sessionInfo.ExpiresAt > DateTime.UtcNow)
            {
                return sessionInfo.Token;
            }
            
            // Remove expired session if found
            if (_activeSessions.ContainsKey(userId))
            {
                _activeSessions.TryRemove(userId, out _);
                Console.WriteLine($"Removed expired session for user {userId} while trying to get token");
            }
            
            return string.Empty;
        }
        
        // Check if a token is valid
        public bool IsTokenValid(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;
                
            // First clean up expired sessions
            CleanupExpiredSessions();
            
            // Check if token exists in any active session
            return _activeSessions.Any(session => session.Value.Token == token && session.Value.ExpiresAt > DateTime.UtcNow);
        }
        
        // Clean up expired sessions
        public void CleanupExpiredSessions()
        {
            var now = DateTime.UtcNow;
            var expiredSessions = 0;
            
            foreach (var session in _activeSessions)
            {
                if (session.Value.ExpiresAt < now)
                {
                    _activeSessions.TryRemove(session.Key, out _);
                    expiredSessions++;
                }
            }
            
            if (expiredSessions > 0)
            {
                Console.WriteLine($"Cleaned up {expiredSessions} expired sessions");
                Console.WriteLine($"Remaining active sessions: {_activeSessions.Count}");
            }
        }
    }
}
