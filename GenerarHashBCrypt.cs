using System;

// ============================================
// Utilidad para generar un hash BCrypt
// ============================================
// Uso (en un proyecto de consola con el paquete BCrypt.Net-Next):
//   dotnet run -- "<contraseña>"
// Si no se pasa la contraseña como argumento, se pide por consola.
// ============================================

namespace TATA.BACKEND.PROYECTO1.Herramientas
{
    public static class GenerarHashBCrypt
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("  GENERADOR DE HASH BCRYPT");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            var textoPlano = args.Length > 0 ? args[0] : LeerDeConsola();
            if (string.IsNullOrWhiteSpace(textoPlano))
            {
                Console.WriteLine("No se ingresó ningún valor.");
                return;
            }

            // Generar hash con BCrypt (workFactor = 11)
            var hash = BCrypt.Net.BCrypt.HashPassword(textoPlano, 11);
            Console.WriteLine("Hash BCrypt generado:");
            Console.WriteLine(hash);
            Console.WriteLine();

            // Verificar que el hash funciona correctamente
            var esValido = BCrypt.Net.BCrypt.Verify(textoPlano, hash);
            Console.WriteLine($"Verificación del hash: {(esValido ? "VÁLIDO" : "INVÁLIDO")}");
        }

        private static string LeerDeConsola()
        {
            Console.Write("Texto a hashear: ");
            return Console.ReadLine() ?? string.Empty;
        }
    }
}
