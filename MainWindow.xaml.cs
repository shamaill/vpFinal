using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace StudentProgressTracker
{
    public partial class MainWindow : Window
    {
        private readonly string connectionString = @"Data Source=DELL\MSSQLSERVER01;Initial Catalog=StudentProgressTracker;Integrated Security=True;Trust Server Certificate=True";

        public ObservableCollection<Student> Students { get; set; } = new ObservableCollection<Student>();
        public ObservableCollection<string> Grades { get; set; } = new ObservableCollection<string> { "All", "Grade 9", "Grade 10", "Grade 11", "Grade 12" };
        public ObservableCollection<string> Subjects { get; set; } = new ObservableCollection<string> { "All", "Mathematics", "Physics", "Chemistry", "Biology", "English" };

        public Student SelectedStudent { get; set; } = new Student();
        public string SelectedGrade { get; set; } = "All";
        public string SelectedSubject { get; set; } = "All";

        public ICommand AddStudentCommand { get; }
        public ICommand UpdateStudentCommand { get; }
        public ICommand DeleteStudentCommand { get; }
        public ICommand FilterStudentsCommand { get; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            LoadStudentsFromDatabase();

            AddStudentCommand = new RelayCommand(AddStudent);
            UpdateStudentCommand = new RelayCommand(UpdateStudent);
            DeleteStudentCommand = new RelayCommand(DeleteStudent);
            FilterStudentsCommand = new RelayCommand(FilterStudents);
        }

        // Load students from the database
        private async void LoadStudentsFromDatabase()
        {
            Students.Clear();
            LoadingProgressBar.Visibility = Visibility.Visible;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT * FROM Students";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        Students.Add(new Student
                        {
                            StudentId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Grade = reader.GetString(2),
                            Subject = reader.GetString(3),
                            Marks = reader.GetInt32(4),
                            AttendancePercentage = reader.GetDouble(5)
                        });
                    }
                }

                StudentDataGrid.ItemsSource = Students;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading students: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingProgressBar.Visibility = Visibility.Hidden;
            }
        }

        // Filter students by Grade & Subject
        private void FilterStudents()
        {
            var filteredList = Students.Where(s =>
                (SelectedGrade == "All" || s.Grade == SelectedGrade) &&
                (SelectedSubject == "All" || s.Subject == SelectedSubject)
            ).ToList();

            StudentDataGrid.ItemsSource = new ObservableCollection<Student>(filteredList);
        }

        // Add a new student record
        private async void AddStudent()
        {
            if (SelectedStudent == null)
            {
                SelectedStudent = new Student();
            }

            if (string.IsNullOrWhiteSpace(SelectedStudent.Name) ||
                string.IsNullOrWhiteSpace(SelectedStudent.Grade) ||
                string.IsNullOrWhiteSpace(SelectedStudent.Subject) ||
                SelectedStudent.Marks < 0 ||
                SelectedStudent.AttendancePercentage < 0 || SelectedStudent.AttendancePercentage > 100)
            {
                MessageBox.Show("Please enter valid student details.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "INSERT INTO Students (Name, Grade, Subject, Marks, AttendancePercentage) VALUES (@Name, @Grade, @Subject, @Marks, @Attendance); SELECT SCOPE_IDENTITY();";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Name", SelectedStudent.Name ?? "Unknown");
                    cmd.Parameters.AddWithValue("@Grade", SelectedStudent.Grade ?? "Unknown");
                    cmd.Parameters.AddWithValue("@Subject", SelectedStudent.Subject ?? "Unknown");
                    cmd.Parameters.AddWithValue("@Marks", SelectedStudent.Marks);
                    cmd.Parameters.AddWithValue("@Attendance", SelectedStudent.AttendancePercentage);

                    object newId = await cmd.ExecuteScalarAsync();

                    if (newId != null)
                    {
                        SelectedStudent.StudentId = Convert.ToInt32(newId);
                        Students.Add(new Student
                        {
                            StudentId = SelectedStudent.StudentId,
                            Name = SelectedStudent.Name,
                            Grade = SelectedStudent.Grade,
                            Subject = SelectedStudent.Subject,
                            Marks = SelectedStudent.Marks,
                            AttendancePercentage = SelectedStudent.AttendancePercentage
                        });
                    }
                }

                MessageBox.Show("Student added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding student: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Update an existing student record
        private async void UpdateStudent()
        {
            if (StudentDataGrid.SelectedItem is Student selectedStudent)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        string query = "UPDATE Students SET Name=@Name, Grade=@Grade, Subject=@Subject, Marks=@Marks, AttendancePercentage=@Attendance WHERE StudentId=@StudentId";

                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@StudentId", selectedStudent.StudentId);
                        cmd.Parameters.AddWithValue("@Name", selectedStudent.Name);
                        cmd.Parameters.AddWithValue("@Grade", selectedStudent.Grade);
                        cmd.Parameters.AddWithValue("@Subject", selectedStudent.Subject);
                        cmd.Parameters.AddWithValue("@Marks", selectedStudent.Marks);
                        cmd.Parameters.AddWithValue("@Attendance", selectedStudent.AttendancePercentage);

                        await cmd.ExecuteNonQueryAsync();
                    }
                    LoadStudentsFromDatabase();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating student: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Delete a student record
        private async void DeleteStudent()
        {
            if (StudentDataGrid.SelectedItem is Student selectedStudent)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        string query = "DELETE FROM Students WHERE StudentId=@StudentId";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@StudentId", selectedStudent.StudentId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    Students.Remove(selectedStudent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting student: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class Student
    {
        public int StudentId { get; set; }
        public string? Name { get; set; }
        public string? Grade { get; set; }
        public string? Subject { get; set; }
        public int Marks { get; set; }
        public double AttendancePercentage { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
