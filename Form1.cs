using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace BASA
{
    public partial class Form1 : Form
    {
        DataSet ds;
        SqlDataAdapter adapter;
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["InsuranceDB"].ConnectionString;
        string sql = "SELECT * FROM InsuranceProgram";

        public Form1()
        {
            InitializeComponent();
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.AllowUserToAddRows = false;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged_1;
            Loading();

        }

        public void Loading()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                adapter = new SqlDataAdapter(sql, connection);
                ds = new DataSet();
                adapter.Fill(ds);
                dataGridView1.DataSource = ds.Tables[0];
            }
            dataGridView1.Columns["ProgramID"].HeaderText = "ID Программы";
            dataGridView1.Columns["Name"].HeaderText = "Название Программы";
            dataGridView1.Columns["CostFormula"].HeaderText = "Формула расчёта";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ProgramID, Name FROM InsuranceProgram";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                comboBox1.DataSource = dt;
                comboBox1.DisplayMember = "Name";
                comboBox1.ValueMember = "ProgramID";
            }
            LoadCaseTypes();
            LoadEditableData();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DataRow row = ds.Tables[0].NewRow();
            ds.Tables[0].Rows.Add(row);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                adapter = new SqlDataAdapter(sql, connection);
                SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);
                adapter.Update(ds);
                ds.Clear();
                adapter.Fill(ds);
            }
        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (comboBox1.SelectedValue == null) return;

            int selectedProgramID;
            if (int.TryParse(comboBox1.SelectedValue.ToString(), out selectedProgramID))
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                   
                    string query = @"
                SELECT 
                    Contract.ContractID, 
                    Client.FullName AS ClientName, 
                    InsuranceAgent.FullName AS AgentName, 
                    Contract.Cost, 
                    Contract.StartDate, 
                    Contract.EndDate 
                FROM 
                    Contract
                INNER JOIN 
                    Client ON Contract.ClientID = Client.ClientID
                INNER JOIN 
                    InsuranceAgent ON Contract.InsuranceAgentID = InsuranceAgent.InsuranceAgentID
                WHERE 
                    Contract.ProgramID = @ProgramID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ProgramID", selectedProgramID);
                    SqlDataReader reader = command.ExecuteReader();

                    
                    dataGridView3.Columns.Clear();
                    dataGridView3.Rows.Clear();

                    
                    dataGridView3.Columns.Add("ContractID", "ID договора");
                    dataGridView3.Columns.Add("ClientName", "Имя клиента");
                    dataGridView3.Columns.Add("AgentName", "Имя агента");
                    dataGridView3.Columns.Add("Cost", "Стоимость контракта");
                    dataGridView3.Columns.Add("StartDate", "Дата начала");
                    dataGridView3.Columns.Add("EndDate", "Дата окончания");

                    
                    while (reader.Read())
                    {
                        dataGridView3.Rows.Add(
                            reader["ContractID"],
                            reader["ClientName"],
                            reader["AgentName"],
                            reader["Cost"],
                            reader["StartDate"],
                            reader["EndDate"]
                        );
                    }
                }
            }
        }


        private void button4_Click(object sender, EventArgs e)
        {
            int year;
            if (!int.TryParse(textBox1.Text, out year))
            {
                MessageBox.Show("Введите корректный год.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("GetAnnualReport", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Year", year);

                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        dataGridView2.Columns.Clear();
                        dataGridView2.Rows.Clear();

                        dataGridView2.Columns.Add("TotalInsuranceCases", "Всего страховых случаев");
                        dataGridView2.Columns.Add("TotalPayout", "Общая сумма выплат");
                        dataGridView2.Columns.Add("TotalContractCost", "Общая стоимость контрактов");

                        if (reader.Read())
                        {
                            dataGridView2.Rows.Add(
                                reader["TotalInsuranceCases"],
                                reader["TotalPayout"],
                                reader["TotalContractCost"]
                            );
                        }

                        
                        if (reader.NextResult())
                        {
                            
                            dataGridView2.Columns.Add("MostProfitableProgramName", "Самая прибыльная программа");
                            dataGridView2.Columns.Add("Profit", "Прибыль");

                            if (reader.Read())
                            {
                               
                                dataGridView2.Rows[0].Cells["MostProfitableProgramName"].Value = reader["MostProfitableProgramName"];
                                dataGridView2.Rows[0].Cells["Profit"].Value = reader["Profit"];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }


        private void LoadCaseTypes()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT CaseTypeID, Situation FROM CaseType";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable caseTypes = new DataTable();
                adapter.Fill(caseTypes);

                comboBox2.DataSource = caseTypes;
                comboBox2.DisplayMember = "Situation";
                comboBox2.ValueMember = "CaseTypeID";
            }
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            DateTime selectedDate = dateTimePicker1.Value.Date;
            int selectedCaseTypeID;

            if (!int.TryParse(comboBox2.SelectedValue.ToString(), out selectedCaseTypeID))
            {
                MessageBox.Show("Пожалуйста, выберите корректный тип случая.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT IC.CaseID, IC.ContractID, IC.PayoutAmount, IC.Date, IC.PayoutCount, CT.Situation 
                         FROM InsuranceCase IC 
                         INNER JOIN CaseType CT ON IC.CaseTypeID = CT.CaseTypeID 
                         WHERE IC.Date = @Date AND IC.CaseTypeID = @CaseTypeID";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Date", selectedDate);
                command.Parameters.AddWithValue("@CaseTypeID", selectedCaseTypeID);

                SqlDataReader reader = command.ExecuteReader();

                dataGridView4.Columns.Clear();
                dataGridView4.Rows.Clear();

                dataGridView4.Columns.Add("CaseID", "ID случая");
                dataGridView4.Columns.Add("ContractID", "ID контракта");
                dataGridView4.Columns.Add("PayoutAmount", "Сумма выплаты");
                dataGridView4.Columns.Add("Date", "Дата");
                dataGridView4.Columns.Add("PayoutCount", "Количество выплат");
                dataGridView4.Columns.Add("Situation", "Тип случая");

                while (reader.Read())
                {
                    dataGridView4.Rows.Add(
                        reader["CaseID"],
                        reader["ContractID"],
                        reader["PayoutAmount"],
                        reader["Date"],
                        reader["PayoutCount"],
                        reader["Situation"]
                    );
                }
            }
        }
        private void LoadEditableData()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT CaseID, ContractID, PayoutAmount, Date, PayoutCount, CaseTypeID 
                         FROM InsuranceCase";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);


                dataGridView5.DataSource = dataTable;

                dataGridView5.ReadOnly = false;
                dataGridView5.Columns["CaseID"].ReadOnly = true;
                dataGridView5.Columns["ContractID"].HeaderText = "ID договора";
                dataGridView5.Columns["PayoutAmount"].HeaderText = "Размер выплаты";
                dataGridView5.Columns["Date"].HeaderText = "Дата";
                dataGridView5.Columns["PayoutCount"].HeaderText = "Количество выплат";
                dataGridView5.Columns["CaseTypeID"].HeaderText = "ID  случая";
            }
        }

        private void buttonSaveChanges_Click(object sender, EventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand maxCaseIdCommand = new SqlCommand("SELECT ISNULL(MAX(CaseID), 0) FROM InsuranceCase", connection);
                int maxCaseID = (int)maxCaseIdCommand.ExecuteScalar();
                int newCaseID = maxCaseID + 1;

                string query = @"SELECT CaseID, ContractID, PayoutAmount, Date, PayoutCount, CaseTypeID 
                         FROM InsuranceCase";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);

                DataTable changes = ((DataTable)dataGridView5.DataSource).GetChanges();

                if (changes != null)
                {
                    try
                    {
                        foreach (DataRow row in changes.Rows)
                        {
                            if (row.RowState == DataRowState.Added)
                            {
                                row["CaseID"] = newCaseID++;
                            }
                        }

                        adapter.Update(changes);
                        MessageBox.Show("Изменения успешно сохранены.");

                        ((DataTable)dataGridView5.DataSource).AcceptChanges();
                        RefreshDataGridView5();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Нет изменений для сохранения.");
                }
            }
        }

        private void RefreshDataGridView5()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"SELECT CaseID, ContractID, PayoutAmount, Date, PayoutCount, CaseTypeID 
                         FROM InsuranceCase";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                dataGridView5.DataSource = dataTable;

                dataGridView5.ReadOnly = false;
                dataGridView5.Columns["CaseID"].ReadOnly = true; // ID не редактируется
            }
        }


        private void textBox3_TextChanged(object sender, EventArgs e)
        {
        }
        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
