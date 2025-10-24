using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace POE_CMCS
{
    public partial class AcademicManagerWindow : Window
    {
        private string ConnectionString = @"Server=LabVM1846780\SQLEXPRESS;Database=POE_CMCS;Integrated Security=True;";

        public AcademicManagerWindow()
        {
            InitializeComponent();
            LoadClaims();
        }

        public class Claim
        {
            public int ClaimId { get; set; }
            public string LecturerName { get; set; }
            public decimal HoursWorked { get; set; }
            public decimal HourlyRate { get; set; }
            public string Notes { get; set; }
            public string FileName { get; set; }
            public string Status { get; set; }
            public DateTime DateSubmitted { get; set; }
        }

        private async void LoadClaims()
        {
            try
            {
                List<Claim> claimsList = new List<Claim>();

                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT ClaimId, LecturerUsername, HoursWorked, HourlyRate, Notes, FileName, Status, DateSubmitted FROM Claims";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            claimsList.Add(new Claim
                            {
                                ClaimId = reader.GetInt32(0),
                                LecturerName = reader.GetString(1),
                                HoursWorked = reader.GetDecimal(2),
                                HourlyRate = reader.GetDecimal(3),
                                Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                FileName = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Status = reader.IsDBNull(6) ? "Pending" : reader.GetString(6),
                                DateSubmitted = reader.GetDateTime(7)
                            });
                        }
                    }
                }

                ClaimsDataGrid.ItemsSource = claimsList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading claims: " + ex.Message);
            }
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClaimsDataGrid.SelectedItem is Claim selectedClaim)
            {
                await UpdateClaimStatus(selectedClaim.ClaimId, "Approved by Coordinator");
                LoadClaims();
            }
        }

        private async void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClaimsDataGrid.SelectedItem is Claim selectedClaim)
            {
                await UpdateClaimStatus(selectedClaim.ClaimId, "Rejected");
                LoadClaims();
            }
        }

        private async void FinalApproveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClaimsDataGrid.SelectedItem is Claim selectedClaim)
            {
                await UpdateClaimStatus(selectedClaim.ClaimId, "Final Approved for Payment");
                LoadClaims();
            }
        }

        private async Task UpdateClaimStatus(int claimId, string status)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    string query = "UPDATE Claims SET Status = @status WHERE ClaimId = @claimId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@claimId", claimId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show($"Claim {claimId} updated to '{status}'.", "Status Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating claim: " + ex.Message);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadClaims();
        }

        // Open uploaded PDF
        private async void OpenPDF_Click(object sender, RoutedEventArgs e)
        {
            if (ClaimsDataGrid.SelectedItem is Claim selectedClaim)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(ConnectionString))
                    {
                        await conn.OpenAsync();
                        string query = "SELECT [Supporting Doc], FileName FROM Claims WHERE ClaimId = @id";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedClaim.ClaimId);

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
