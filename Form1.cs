using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace AvtoService
{
    public partial class Form1 : Form
    {
        string connectionString = "server=localhost;database=avtoservice;uid=root;pwd=root";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //загрузка данных в таблицы
            LoadClients();
            LoadCars();
            LoadOrders();
            LoadServices();
            LoadStatus();

            dataGridChoose.DefaultCellStyle.Font = new Font("Segoe UI", 10); // или 8, если нужно мельче
            dataGridChoose.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        }

        // инициализация данных
        public void LoadClients()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                // загрузка данных
                connection.Open();
                var adapter = new MySqlDataAdapter("select *from clients",connection);
                var table = new DataTable();
                adapter.Fill(table);
                dataGridViewClients.DataSource = table;
                dataGridViewClients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                // настройка отображения столбцов
                dataGridViewClients.Columns["Client_id"].Visible = false;
                dataGridViewClients.Columns["lastname"].HeaderText = "Фамилия";
                dataGridViewClients.Columns["firstname"].HeaderText = "Имя";
                dataGridViewClients.Columns["surname"].HeaderText = "Отчество";
                dataGridViewClients.Columns["contact_info"].HeaderText = "Контактная информация";
            }
        }   //загрузка таблицы клиентов
        public void LoadCars()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                // загрузка данных
                connection.Open();
                var adapter = new MySqlDataAdapter("select *from cars", connection);
                var table = new DataTable();
                adapter.Fill(table);
                dataGridCars.DataSource = table;
                dataGridCars.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                // настройка отображения столбцов
                dataGridCars.Columns["Car_id"].Visible=false;
                dataGridCars.Columns["Client_id"].Visible = false;
                dataGridCars.Columns["Car_brand"].HeaderText = "Марка авто";
                dataGridCars.Columns["Car_model"].HeaderText = "Модель авто";
                dataGridCars.Columns["Car_number"].HeaderText = "Номер авто";
            }
        }       //загрузка таблицы авто
        public void LoadOrders()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                // загрузка данных
                connection.Open();
                var adapter = new MySqlDataAdapter("select *from orders", connection);
                var table = new DataTable();
                adapter.Fill(table);
                dataGridOrders.DataSource = table;
                // настройка отображения столбцов
                dataGridOrders.Columns["notes"].Width = 300;
                dataGridOrders.Columns["order_id"].HeaderText = "Номер заказа";
                dataGridOrders.Columns["Car_id"].Visible = false;
                dataGridOrders.Columns["order_date"].HeaderText = "Дата заказа";
                dataGridOrders.Columns["status"].HeaderText = "Статус";
                dataGridOrders.Columns["notes"].HeaderText = "Дополнения";
            }
        }           //загрузка таблицы владельцев
        public void LoadServices()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var cmd = new MySqlCommand("select service_id, service_name from services ", connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32("service_id");
                    string name = reader.GetString("service_name");
                    clbServices.Items.Add(new KeyValuePair<int, string>(id, name));
                }
                clbServices.DisplayMember = "Value";
            }
        }   //загрузка услуг в список
        public void LoadStatus()
        {
            cbStatus.Items.Clear();
            cbStatus.Items.AddRange(new[] {"Принят","В работе","Готов","Выдан" });
            cbStatus.SelectedIndex = 0;
        }

        //кнопки
        private void buttonFilter_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtOrderId.Text, out int orderId))
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT o.order_id, o.order_date, o.status,
                       c.car_number, cl.lastname, cl.firstname
                FROM Orders o
                JOIN Cars c ON o.car_id = c.car_id
                JOIN Clients cl ON c.client_id = cl.client_id
                WHERE o.order_id = @orderId";

                    var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@orderId", orderId);

                    var adapter = new MySqlDataAdapter(cmd);
                    var table = new DataTable();
                    adapter.Fill(table);
                    dataGridChoose.DataSource = table;
                    dataGridChoose.Visible = true;
                    dataGridChoose.Columns["order_id"].HeaderText = "Номер заказа";
                    dataGridChoose.Columns["order_date"].HeaderText = "Дата заказа";
                    dataGridChoose.Columns["status"].HeaderText = "Статус";
                    dataGridChoose.Columns["Car_number"].HeaderText = "Номер авто";
                    dataGridChoose.Columns["lastname"].HeaderText = "Фамилия";
                    dataGridChoose.Columns["firstname"].HeaderText = "Имя";
                }
            }
            else
            {
                MessageBox.Show("Введите корректный номер заказа.");
            }
        }   //поиск заказа по номеру
        private void buttonClear_Click(object sender, EventArgs e)
        {
            txtOrderId.Clear();
            dataGridChoose.Visible = false;
        }

        private void btnAddFullOrder_Click(object sender, EventArgs e)  //добавить
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                try
                {
                    //добавление клиента
                    var cmdclient = new MySqlCommand(@"
                    insert into Clients (lastname, firstname, surname, contact_info)
                    values (@ln, @fn, @sn, @contact)", connection, transaction);
                    cmdclient.Parameters.AddWithValue("@ln", txtLastName.Text);
                    cmdclient.Parameters.AddWithValue("@fn", txtFirstname.Text);
                    cmdclient.Parameters.AddWithValue("@sn",txtSurname.Text);
                    cmdclient.Parameters.AddWithValue("@contact",txtContact.Text);
                    cmdclient.ExecuteNonQuery();
                    int clientId = (int)cmdclient.LastInsertedId;

                    //добавление автомоблиля клиента
                    var cmdCar = new MySqlCommand(@"
                    insert into cars (client_id, car_brand, car_model, car_number)
                    values (@cid, @cb, @cm, @cn)", connection, transaction);
                    cmdCar.Parameters.AddWithValue("@cid", clientId);
                    cmdCar.Parameters.AddWithValue("@cb", txtCarBrand.Text);
                    cmdCar.Parameters.AddWithValue("@cm",txtCarModel.Text);
                    cmdCar.Parameters.AddWithValue("@cn", txtCarNumber.Text);
                    cmdCar.ExecuteNonQuery();
                    int carId  = (int)cmdCar.LastInsertedId;

                    //добавление заказа
                    var cmdOrder = new MySqlCommand(@"
                    insert into orders (car_id, order_date, status, notes)
                    values (@carId, @od, 'принят', @nts)", connection, transaction);
                    cmdOrder.Parameters.AddWithValue("@carId", carId);
                    cmdOrder.Parameters.AddWithValue("@od", dpOrderDate.Value);
                    cmdOrder.Parameters.AddWithValue("@nts", txtNotes.Text);
                    cmdOrder.ExecuteNonQuery();

                    transaction.Commit();
                    MessageBox.Show("Заказ успешно добавлен");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("SQL ошибка: " + ex.Message);
                }
                LoadClients();
                LoadCars();
                LoadOrders();
            }
        }

        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dataGridChoose.CurrentRow != null)
            {
                int orderId = Convert.ToInt32(dataGridChoose.CurrentRow.Cells["order_id"].Value);
                string newStatus = cbStatus.SelectedItem.ToString();

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE Orders SET status = @status WHERE order_id = @id";
                    var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id", orderId);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Статус обновлён!");
                LoadOrders(); // перезагрузка таблицы заказов
                txtOrderId.Clear();
                dataGridChoose.Visible = false;
            }
            else
            {
                MessageBox.Show("Выберите заказ из списка.");
            }
        }

    }
}
