using System;

// ============================================
// Programa para Generar Hash BCrypt
// ============================================
// Para ejecutar:
// 1. Crea un nuevo proyecto de consola: dotnet new console -n HashGenerator
// 2. Instala BCrypt: dotnet add package BCrypt.Net-Next
// 3. Copia este código en Program.cs
// 4. Ejecuta: dotnet run
// ============================================

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  GENERADOR DE HASH BCRYPT");
        Console.WriteLine("===========================================");
        Console.WriteLine();
        
        string password = "123456";
        
        // Generar hash con BCrypt (workFactor = 11)
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, 11);
        
        Console.WriteLine($"Contraseña Original: {password}");
        Console.WriteLine();
        Console.WriteLine($"Hash BCrypt Generado:");
        Console.WriteLine(passwordHash);
        Console.WriteLine();
        
        // Verificar que el hash funciona correctamente
        bool isValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);
        Console.WriteLine($"Verificación del hash: {(isValid ? "? VÁLIDO" : "? INVÁLIDO")}");
        Console.WriteLine();
        Console.WriteLine("===========================================");
        Console.WriteLine("Copia el hash generado para usar en tu script SQL");
        Console.WriteLine("===========================================");
        
        Console.ReadKey();
    }
}
