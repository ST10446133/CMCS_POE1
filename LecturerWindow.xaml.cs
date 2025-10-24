using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace POE_CMCS
{
    public partial class LecturerWindow : Window
    {
        private string Username;
        private string ConnectionString = @"Server=LabVM1846780\SQLEXPRESS;Database=POE_CMCS;Integrated Security=True;";
        private string UploadedFilePath = null;

        public LecturerWindow(string username)
        {
            InitializeComponent();
            Username = username;
            LoadClaims();
        }

        // Upload file button
        private void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Documents (*.pdf;*.docx;*.xlsx)|*.pdf;*.docx;*.xlsx",
                Title = "Select Supporting Document"
            };

            if (dialog.ShowDialog() == true)
            {
                UploadedFilePath = dialog.FileName;
                FileNameTextBlock.Text = $"Selected: {System.IO.Path.GetFileName(UploadedFilePath)}";
            }
        }

        // Submit claim
        private async void SubmitClaim_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(HoursWorkedTextBox.Text) ||
                    string.IsNullOrEmpty(HourlyRateTextBox.Text))
                {
                    SubmitStatusText.Text = "Please fill in all required fields.";
                    SubmitStatusText.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                decimal hours = Convert.ToDecimal(HoursWorkedTextBox.Text);
                decimal rate = Convert.ToDecimal(HourlyRateTextBox.Text);
                string notes = NotesTextBox.Text;

                byte[] fileData = null;
                string fileName = null;

                if (!string.IsNullOrEmpty(UploadedFilePath))
                {
                    fileData = File.ReadAllBytes(UploadedFilePath);
                    fileName = System.IO.Path.GetFileName(UploadedFilePath);
                }

                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    // Note the [Supporting Doc] column with brackets
                    string query = @"INSERT INTO Claims 
                                    (LecturerUsername, HoursWorked, HourlyRate, Notes, [Supporting Doc], FileName, Status, DateSubmitted) 
                                     VALUES (@u, @h, @r, @n, @f, @fn, @s, @d)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", Username);
                        cmd.Parameters.AddWithValue("@h", hours);
                        cmd.Parameters.AddWithValue("@r", rate);
                        cmd.Parameters.AddWithValue("@n", notes);

                        if (fileData != null)
                        {
                            cmd.Parameters.AddWithValue("@f", fileData);   // binary PDF
                            cmd.Parameters.AddWithValue("@fn", fileName);  // store original name
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@f", DBNull.Value);
                            cmd.Parameters.AddWithValue("@fn", DBNull.Value);
                        }

                        cmd.Parameters.AddWithValue("@s", "Pending");
                        cmd.Parameters.AddWithValue("@d", DateTime.Now);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                SubmitStatusText.Text = "Claim submitted successfully!";
                SubmitStatusText.Foreground = System.Windows.Media.Brushes.Green;
                LoadClaims();
            }
            catch (Exception ex)
            {
                SubmitStatusText.Text = "Error submitting claim.";
                SubmitStatusText.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show("Error submitting claim: " + ex.Message);
            }
        }

        // Load claims
        private async void LoadClaims()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    // Include FileName so it can show in DataGrid
                    string query = @"SELECT ClaimId, HoursWorked, HourlyRate, Status, DateSubmitted, FileName
                                     FROM Claims 
                                     WHERE LecturerUsername = @u";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", Username);
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        ClaimsDataGrid.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading claims: " + ex.Message);
            }
        }

        private void RefreshClaims_Click(object sender, RoutedEventArgs e)
        {
            LoadClaims();
        }

        // Open uploaded PDF
        private async void OpenPDF_Click(object sender, RoutedEventArgs e)
        {
            if (ClaimsDataGrid.SelectedItem is DataRowView row)
            {
                int claimId = Convert.ToInt32(row["ClaimId"]);

                try
                {
                    using (SqlConnection conn = new SqlConnection(ConnectionString))
                    {
                        await conn.OpenAsync();
                        string query = "SELECT [Supporting Doc], FileName FROM Claims WHERE ClaimId = @id";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", claimId);

                            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        byte[] fileData = (byte[])reader["Supporting Doc"];
                                        string fileName = reader["FileName"].ToString();
                                        string tempPath = Path.Combine(Path.GetTempPath(), fileName);

                                        File.WriteAllBytes(tempPath, fileData);
                                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true });
                                    }
                                    else
                                    {
                                        MessageBox.Show("No file uploaded for this claim.");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening file: " + ex.Message);
                }
            }
        }
    }
}
