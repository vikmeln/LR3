using System;
using System.Data.Common;
using System.Text;
using MySql.Data.MySqlClient;
namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Getting Connection ...");
            MySqlConnection conn = DBUtils.GetDBConnection();
            try
            {
                Console.WriteLine("Openning Connection ...");
                conn.Open();
                Console.WriteLine("Connection successful!");
                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("\n=== Меню ===");
                    Console.WriteLine("1. Операції лікарів на задану дату");
                    Console.WriteLine("2. Зменшити тариф для категорії на 10%");
                    Console.WriteLine("3. Розрахувати витрати для кожного пацієнта");
                    Console.WriteLine("4. Щомісячна премія для кожного хірурга");
                    Console.WriteLine("5. Хірурги без практики за місяць");
                    Console.WriteLine("6. Переглянути таблицю");
                    Console.WriteLine("0. Вихід");
                    Console.Write("Виберіть опцію: ");
                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            QueryOperationsByDate(conn);
                            break;
                        case "2":
                            UpdateTariffForCategory(conn);
                            break;
                        case "3":
                            CalculateTreatmentCosts(conn);
                            break;
                        case "4":
                            CalculateSurgeonBonuses(conn);
                            break;
                        case "5":
                            FindInactiveSurgeons(conn);
                            break;
                        case "6":
                            ShowTable(conn);
                            break;
                        case "0":
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Невірний вибір.");
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }
            Console.Read();
        }
        private static void QueryOperationsByDate(MySqlConnection conn)
        {
            Console.Write("Введіть дату операції (YYYY-MM-DD): ");
            string dateInput = Console.ReadLine();

            string sql = @"SELECT s.last_name, s.first_name, o.operation_name
                           FROM Visits v
                           JOIN Surgeons s ON v.surgeon_code = s.surgeon_code
                           JOIN Operations o ON v.operation_code = o.operation_code
                           WHERE v.data_operation = @operationDate;";

            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@operationDate", dateInput);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"{reader["last_name"]} {reader["first_name"]}: {reader["operation_name"]}");
            }
        }

        private static void UpdateTariffForCategory(MySqlConnection conn)
        {
            Console.WriteLine("\nДоступні операції:");

            // Виведемо список операцій для вибору
            string fetchOperations = "SELECT operation_name FROM Operations";
            using (MySqlCommand cmdFetch = new MySqlCommand(fetchOperations, conn))
            using (var reader = cmdFetch.ExecuteReader())
            {
                int index = 1;
                var operations = new List<string>();
                while (reader.Read())
                {
                    string operationName = reader.GetString("operation_name");
                    operations.Add(operationName);
                    Console.WriteLine($"{index}. {operationName}");
                    index++;
                }
                reader.Close();

                // Якщо список порожній
                if (operations.Count == 0)
                {
                    Console.WriteLine("Операцій немає в базі.");
                    return;
                }

                Console.Write("\nВиберіть номер операції: ");
                if (int.TryParse(Console.ReadLine(), out int operationIndex) && operationIndex > 0 && operationIndex <= operations.Count)
                {
                    string selectedOperation = operations[operationIndex - 1];
                    Console.WriteLine($"\nВи вибрали: {selectedOperation}");

                    // Оновлення тарифів
                    string sql = @"
            UPDATE Tariffs
            SET operation_cost = operation_cost * 0.9,
                cost_of_one_day_rehabilitation = cost_of_one_day_rehabilitation * 0.9
            WHERE operation_code IN (
                SELECT operation_code
                FROM Operations
                WHERE LOWER(operation_name) = LOWER(@operationName)
            );";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@operationName", selectedOperation);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        Console.WriteLine($"Оновлено {rowsAffected} тарифів для операції '{selectedOperation}'.");
                    }
                }
                else
                {
                    Console.WriteLine("Невірний вибір номера операції.");
                }
            }
        }

        private static void CalculateTreatmentCosts(MySqlConnection conn)
        {
            string sql = @"SELECT p.last_name, p.first_name,
                                  t.operation_cost,
                                  t.cost_of_one_day_rehabilitation,
                                  p.actual_period_rehabilitation,
                                  (t.operation_cost + 
                                   t.cost_of_one_day_rehabilitation * 
                                   CAST(p.actual_period_rehabilitation AS UNSIGNED)) AS total_cost
                           FROM Patients p
                           JOIN Visits v ON p.patient_code = v.patient_code
                           JOIN Tariffs t ON v.operation_code = t.operation_code;";

            MySqlCommand cmd = new MySqlCommand(sql, conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"Пацієнт: {reader["last_name"]} {reader["first_name"]}");
                Console.WriteLine($"  Операція: {reader["operation_cost"]} грн");
                Console.WriteLine($"  Реабілітація: {reader["cost_of_one_day_rehabilitation"]} грн/день × {reader["actual_period_rehabilitation"]} днів");
                Console.WriteLine($"  Всього до сплати: {reader["total_cost"]} грн\n");
            }
        }

        private static void CalculateSurgeonBonuses(MySqlConnection conn)
        {
            string sql = @"SELECT s.last_name, s.first_name,
                                  MONTH(v.data_operation) AS op_month,
                                  SUM(t.operation_cost) * 0.1 AS bonus
                           FROM Visits v
                           JOIN Surgeons s ON v.surgeon_code = s.surgeon_code
                           JOIN Tariffs t ON v.operation_code = t.operation_code
                           GROUP BY s.surgeon_code, MONTH(v.data_operation);";

            MySqlCommand cmd = new MySqlCommand(sql, conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"Хірург: {reader["last_name"]} {reader["first_name"]}, Місяць: {reader["op_month"]}, Премія: {reader["bonus"]} грн");
            }
        }

        private static void FindInactiveSurgeons(MySqlConnection conn)
        {
            string sql = @"SELECT s.last_name, s.first_name
                           FROM Surgeons s
                           WHERE NOT EXISTS (
                               SELECT 1
                               FROM Visits v
                               WHERE v.surgeon_code = s.surgeon_code
                               AND v.data_operation >= DATE_SUB(CURDATE(), INTERVAL 1 MONTH)
                           );";

            MySqlCommand cmd = new MySqlCommand(sql, conn);

            using var reader = cmd.ExecuteReader();
            Console.WriteLine("Хірурги без практики протягом останнього місяця:");
            while (reader.Read())
            {
                Console.WriteLine($"{reader["last_name"]} {reader["first_name"]}");
            }
        }

        private static void ShowTable(MySqlConnection conn)
        {
            Console.Write("Введіть назву таблиці (Surgeons, Patients, Operations, Visits, Tariffs): ");
            string table = Console.ReadLine();

            string sql = $"SELECT * FROM `{table}`";
            MySqlCommand cmd = new MySqlCommand(sql, conn);

            try
            {
                using var reader = cmd.ExecuteReader();
                var colCount = reader.FieldCount;

                Console.WriteLine("=".PadRight(60, '='));
                while (reader.Read())
                {
                    for (int i = 0; i < colCount; i++)
                        Console.Write($"{reader.GetName(i)}: {reader.GetValue(i)} | ");
                    Console.WriteLine();
                }
                Console.WriteLine("=".PadRight(60, '='));
            }
            catch (Exception e)
            {
                Console.WriteLine("Помилка при читанні таблиці: " + e.Message);
            }
        }
    }
}