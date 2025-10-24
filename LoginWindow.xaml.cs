using System;
using System.Data.SqlClient;
using System.Windows;

namespace POE_CMCS
{
    public partial class LoginWindow : Window
    {
        private string connectionString = @"Data Source=LabVM1846780\SQLEXPRESS;Initial Catalog=POE_CMCS;Integrated Security=True;";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
            string selectedRole = ((System.Windows.Controls.ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString().Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(selectedRole))
            {
                MessageBox.Show("Please fill in all fields.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "SELECT Role FROM Users WHERE Username = @Username AND Password = @Password";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);

                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        string dbRole = result.ToString().Trim();

                        if (!string.Equals(dbRole?.Trim(), selectedRole?.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show($"Selected role '{selectedRole}' does not match your account role '{dbRole}'.",
                                            "Role Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }


                        switch (dbRole.ToLower())
                        {
                            case "lecturer":
                                new LecturerWindow(username).Show();
                                break;

                            case "programme coordinator":
                                new ProgrammeCoordinatorWindow().Show();
                                break;

                            case "academic manager":
                                new AcademicManagerWindow().Show();
                                break;

                            default:
                                MessageBox.Show("Unknown role. Please contact your system administrator.",
                                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                        }

                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
