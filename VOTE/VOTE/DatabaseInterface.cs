﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BCrypt.Net;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VOTE
{
    public class DatabaseInterface
    {

        private static DatabaseInterface instance = null;

        private String connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TestDB"].ConnectionString;
        private SqlConnection connection;

        private DatabaseInterface()
        {
            connection = new SqlConnection(connectionString);
        }

        ~DatabaseInterface()
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        public static DatabaseInterface getInstance()
        {
            if (instance == null)
            {
                instance = new DatabaseInterface();
            }

            return instance;
        }

        public void storeOption(Option option)
        {
            connection.Open();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO [Option](questionID, [option]) VALUES(@questionID, @option)";
                    command.Parameters.AddWithValue("@questionID", option.QuestionId);
                    command.Parameters.AddWithValue("@option", option.OptionText);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        public void storeQuestion(Question question)
        {
            connection.Open();
            int modified = 0;
            try
            {
                using(SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Question(ballotID, question) OUTPUT INSERTED.questionID VALUES(@ballotID, @question)";
                    command.Parameters.AddWithValue("@ballotID", question.BallotId);
                    command.Parameters.AddWithValue("@question", question.QuestionText);

                    modified = (int)command.ExecuteScalar();
                }
            }
            catch
            {

            }
            finally
            {
                connection.Close();
            }
            foreach (Option option in question.Options)
            {
                option.QuestionId = modified;
                storeOption(option);

            }
        }


        public void storeBallot(Ballot ballot)
        {
            connection.Open();
            int modified = 0;

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Ballot(ballotName, openDate, dueDate) OUTPUT INSERTED.ballotID VALUES(@ballotName, @openDate, @dueDate)";
                    command.Parameters.AddWithValue("@ballotName", ballot.Name);
                    command.Parameters.AddWithValue("@openDate", ballot.OpenDate);
                    command.Parameters.AddWithValue("@dueDate", ballot.DueDate);

                    modified = (int)command.ExecuteScalar();
                }
            }
            catch
            {

            }
            finally
            {
                connection.Close();
            }
            foreach (Question question in ballot.Questions)
            {
                question.BallotId = modified;
                storeQuestion(question);

            }
        }

        public void storeVote(Vote vote)
        {
            connection.Open();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Vote(optionId, questionId, state, age, gender, race) VALUES(@optionId, @questionId, @userstate, @userage, @usergender, @userrace)";
                    command.Parameters.AddWithValue("@optionId", vote.OptionID);
                    command.Parameters.AddWithValue("@questionId", vote.QuestionID);
                    command.Parameters.AddWithValue("@userstate", vote.UserState);
                    command.Parameters.AddWithValue("@userage", vote.UserAge);
                    command.Parameters.AddWithValue("@usergender", vote.UserGender);
                    command.Parameters.AddWithValue("@userrace", vote.UserRace);

                    command.ExecuteNonQuery();
                }
            }
            catch
            {

            }
            finally
            {
                connection.Close();
            }
        }

        public void storeUserBallot(User user, Ballot ballot, DateTime timestamp)
        {
            connection.Open();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO User_Ballot(userId, ballotId, timestamp) VALUES(@userId, @ballotId, @timestamp)";
                    command.Parameters.AddWithValue("@userId", user.UserID);
                    command.Parameters.AddWithValue("@ballotId", ballot.BallotId);
                    command.Parameters.AddWithValue("@timestamp", timestamp);

                    command.ExecuteNonQuery();
                }
            }
            catch
            {

            }
            finally
            {
                connection.Close();
            }
        }

        public void changeBallotDueDate(Ballot ballot, DateTime date)
        {
            connection.Open();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Ballot SET dueDate = @dueDate WHERE ballotID = @ballotID";
                    command.Parameters.AddWithValue("@ballotID", ballot.BallotId);
                    command.Parameters.AddWithValue("@dueDate", date);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {

            }
            finally
            {
                connection.Close();
            }
        }

        public void createNewUser(User user)
        {
            connection.Open();
           
            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO [User](username, password, administrator, state, birthdate, gender, race) VALUES (@username, @password, @administrator, @state, @birthdate, @gender, @race)";
                    command.Parameters.AddWithValue("@username", user.Username);
                    command.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(user.Password, BCrypt.Net.BCrypt.GenerateSalt(12)));
                    command.Parameters.AddWithValue("@administrator", 0);
                    command.Parameters.AddWithValue("@state", user.State);
                    command.Parameters.AddWithValue("@birthdate", user.DateOfBirth);
                    command.Parameters.AddWithValue("@gender", user.Gender);
                    command.Parameters.AddWithValue("@race", user.Race);
                    command.ExecuteNonQuery();
                }

            }
            catch
            {

            }
            finally
            {

                connection.Close();
            }
        }

        public List<Ballot> getAllBallots()
        {
            connection.Open();

            List<Ballot> ballots = new List<Ballot>();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM [Ballot]";

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int ballotId = reader.GetInt32(0);
                            string ballotName = reader.GetString(1);
                            DateTime openDate = reader.GetDateTime(2);
                            DateTime dueDate = reader.GetDateTime(3);

                            Ballot ballot = new Ballot(ballotId, ballotName, openDate, dueDate);
                           

                            ballots.Add(ballot);
                        }
                    }
                }
            }
            catch
            {

            }
            finally
            {
                connection.Close();
            }
            foreach(Ballot ballot in ballots)
            {
                ballot.Questions = getQuestions(ballot);
            }

            return ballots;
        }

        public List<Ballot> getActiveBallots(User user)
        {
            connection.Open();

            List<Ballot> ballots = new List<Ballot>();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM [Ballot] WHERE OpenDate < @now AND DueDate > @now AND BallotId NOT IN " +
                        "(SELECT BallotId FROM [User_Ballot] WHERE UserId = @userId);";
                    command.Parameters.AddWithValue("@now", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@userId", user.UserID);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int ballotId = reader.GetInt32(0);
                            string ballotName = reader.GetString(1);
                            DateTime openDate = reader.GetDateTime(2);
                            DateTime dueDate = reader.GetDateTime(3);

                            Ballot ballot = new Ballot(ballotId, ballotName, openDate, dueDate);
                            

                            ballots.Add(ballot);
                        }
                    }
                }
            }
            catch
            {

            }
            finally
            {
                connection.Close();
            }
            foreach(Ballot ballot in ballots)
            {
                ballot.Questions = getQuestions(ballot);
            }

            return ballots;
        }

        public List<Question> getQuestions(Ballot ballot)
        {
            connection.Open();

            List<Question> questions = new List<Question>();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM [Question] WHERE [ballotID]=@ballotID";
                    command.Parameters.AddWithValue("@ballotID", ballot.BallotId);

                    using(SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int questionID = reader.GetInt32(0);
                            int ballotID = reader.GetInt32(1);
                            String question = reader.GetString(2);

                            Question newQuestion = new Question(questionID, ballotID, question);

                            questions.Add(newQuestion);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                connection.Close();
            }
            foreach(Question question in questions)
            {
                question.Options = GetOptions(question);
            }
            return questions;
        }

        public List<Option> GetOptions(Question question)
        {
            connection.Open();

            List<Option> options = new List<Option>();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM [Option] WHERE questionID=@questionID";
                    command.Parameters.AddWithValue("@questionID", question.QuestionId);

                    using(SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int questionId = reader.GetInt32(0);
                            int optionId = reader.GetInt32(1);
                            String option = reader.GetString(2);

                            Option newOption = new Option(optionId, questionId, option);
                            options.Add(newOption);
                        }
                    }
                }
            }
            catch
            {

            }
            finally
            {
               connection.Close();
            }
            return options;
        }

        public List<Vote> getVotes(Ballot ballot)
        {
            connection.Open();

            List<Vote> votes = new List<Vote>();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    string questionIDs = "";
                    List<int> questionIDList = new List<int>();
                    foreach(Question question in ballot.Questions)
                    {
                        questionIDList.Add(question.QuestionId);
                    }

                    if (questionIDList.Count() > 0)
                    {
                        questionIDs = String.Join(", ", questionIDList);
                        command.CommandText = "SELECT * FROM [Vote] WHERE [questionID] IN (" + questionIDs + ")";
                        command.Parameters.AddWithValue("@ballotID", ballot.BallotId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int optionID = reader.GetInt32(0);
                                int questionID = reader.GetInt32(1);
                                string state = reader.GetString(2);
                                int age = reader.GetInt32(3);
                                string gender = reader.GetString(4);
                                string race = reader.GetString(5);
                                int voteID = reader.GetInt32(6);

                                Vote newVote = new Vote(voteID, optionID, questionID, state, age, gender, race);

                                votes.Add(newVote);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                connection.Close();
            }

            return votes;
        }

        public User authenticateUser(string username, string password)
        {
            connection.Open();

            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM [User] WHERE username=@username";
                    command.Parameters.AddWithValue("@username", username);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            String usernameDb = reader.GetString(1).TrimEnd();
                            String passwordDb = reader.GetString(2);
                            String state = reader.GetString(4).TrimEnd();
                            DateTime dateOfBirth = reader.GetDateTime(5);
                            String gender = reader.GetString(6).TrimEnd();
                            String race = reader.GetString(7).TrimEnd();
                            Boolean administrator = reader.GetBoolean(3);

                            User user = new User(userId, usernameDb, passwordDb, state, dateOfBirth.ToString("yyyy-MM-dd"), gender, race, administrator);

                            if (BCrypt.Net.BCrypt.Verify(password, user.Password))
                            {
                                return user;
                            }
                        }
                    }
                }
            } catch (SqlException e)
            {
                MessageBox.Show(e.Message);
            } finally
            {
                connection.Close();
            }

            return null;
        }

        public Boolean doesUserExist(string username)
        {
            connection.Open();
            try
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT count(*) FROM [User] WHERE username=@username";
                    command.Parameters.AddWithValue("@username", username);

                    int userCount = (int)command.ExecuteScalar();

                    if (userCount > 0)
                    {
                        return true;
                    }
                }
            }
            catch
            {

            }
            finally
            {
                connection.Close();
            }

            return false;
        }
    }
}
