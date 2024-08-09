using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;

namespace AutogenerateOptions.Lib.Engine;

public static class SettingsGeneratorExtensions
{
    /// <summary>
    /// Добавление функционала автокодогенерации опций из конфигов
    /// </summary>
    public static void AddAutogenerateOptions(this IServiceCollection services, params string[]  excludeSections)
    {
        services.AddSingleton<ITypeNameGenerator, DbTypeNameGenerator>();

        var className = "ServiceOptions";
        var folderName = "Options";

        Directory.CreateDirectory(folderName);

        // Определяем запускаемый проект по классу Program. Вместе с этим определяем namespace, и название файла для автокодогенерации
        var programPath = GetPathToProgram(AppContext.BaseDirectory);
        var folderPath = Path.Combine(programPath, folderName);
        var nameSpace = GetNamespace(programPath);
        var fileName = Path.Combine(folderPath, $"{className}.cs");

        // Забираем конфиги и мержим их относительно нужного environmentName
        var baseSettings = File.ReadAllText("appsettings.json");

        var baseJsonObject = JObject.Parse(baseSettings);

        var files = Directory.GetFiles(programPath, "appsettings.*.json");

        foreach(var file in files)
        {
            var envSettings = File.ReadAllText(file);
            var envJsonObject = JObject.Parse(envSettings);
            baseJsonObject.Merge(envJsonObject);
        }

        // Получаем смерженый json наших конфигов
        foreach (var excludeSection in excludeSections)
        {
            baseJsonObject.Remove(excludeSection);
        }
        var json = baseJsonObject.ToString();


        // Строим схему
        var schemaGenerator = new JsonSchemaGenerator();
        var schema = schemaGenerator.Generate(json);
        schema.Title = className;

        // Генерируем файл конфигов
        var generatorSettings = new CSharpGeneratorSettings
        {
            Namespace = nameSpace,
            ClassStyle = CSharpClassStyle.Poco,
            JsonLibrary = CSharpJsonLibrary.SystemTextJson,
            TypeNameGenerator = new DbTypeNameGenerator()
        };

        var generator = new CSharpGenerator(schema, generatorSettings);

        var generatedFile = generator.GenerateFile();
        generatedFile = generatedFile.Replace("\n\n\n", "\n");

        // Физически создаем этот файл в решении
        File.WriteAllText(fileName, generatedFile);
    }

    /// <summary>
    /// Нахождение пути в решении где лежит файл Program
    /// </summary>
    private static string GetPathToProgram(string path)
    {
        var directory = Directory.GetParent(path);

        if (directory == null)
            throw new Exception("Задан некорректный путь");

        var directoryPath = directory.FullName;

        var files = Directory.GetFiles(directoryPath, searchPattern: "*.cs");

        foreach (var file in files)
        {
            if(file.EndsWith("Program.cs"))
                return directoryPath;
        }

        return GetPathToProgram(directoryPath);
    }

    /// <summary>
    /// Определение неймспейса класса Program исходя из структуры папок проекта
    /// </summary>
    private static string GetNamespace(string programPath)
    {
        var directory = Directory.GetParent(programPath);

        if (directory == null)
            throw new Exception("Задан некорректный путь");

        var directoryPath = directory.FullName;

        var files = Directory.GetFiles(programPath, searchPattern: "*.cs");

        foreach (var file in files)
        {
            if (file.EndsWith("Startup.cs"))
            {
                var filename = Path.GetFileNameWithoutExtension(file);

                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .Reverse()
                    .SelectMany(assembly => assembly.GetTypes())
                    .FirstOrDefault(t => t.Name == filename);

                if (type != null && !string.IsNullOrEmpty(type.Namespace))
                    return $"{type.Namespace}.Options";
            }
        }

        var lastIndex = directoryPath.LastIndexOf('\\') + 1;

        var result = $"{directoryPath.Substring(lastIndex)}.Options";

        return result;
    }
}

