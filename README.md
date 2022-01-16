# Twitter Simulator Part 2

## Group Members:
1. Pranali Suhas Narkar
2. Manish Alluri

## Project overview
In the first part of this project, we had developed REST APIs to simulate some of the features of twitter. For this we had used HTTP server feature from Akka.Net framework to expose our API calls.
Along with this, we had used Akka.Net actors to simulate certain number of client users to load test our server side APIs.

In this part of our project, we have used Suave.IO framework istead of Akka.Net to develope and expose our REST APIs. We have also used Websocket from Suave.IO framework to push all the updates in realtime to the connected user clients.
On the client side, we have provided two options to interact with the Server side APIs.
  1. Simulator
  2. Console Shell
  
### Simulator
This mode is used to simulate workload of multiple users when they try to access our APIs. For this we are using actors from Akka.Net framework. Each actor will behave as an individual user 
and will make certain API calls to the server. Each actor will make an HTTP request instead of sending a renmote message over the akka.net cluster when making a API call. 
We have also integrated websocket with each actor so that they can listen and log all the events in real time.

### Console Shell
This option provides an interactive shell to the user where user can select different options to make different API calls to the server. Following are the available options.
  1. Register - New user accounts can be created using this option. You will have to enter an unique username and a password to register a user. If the username already exists then it shows an error.
  2. Login - Newly registered users can login using this option. User has to enter the username and password that they used during registration. If credentials are valid, then it will create a session on backend and save the session id on client  side else it will show an error.
  3. Follow - Using this option logged in user can follow other users. It will show list of suggested users with top followers. You can enter any username you want to follow. If that username exists, then it will follow that user else it will show an error.
  4. Post a tweet - User can post a new tweet using this option. User can add new or existing hashtags to the tweet and also mention any other if they want. Once the tweet is successfully posted, we are pushing the notification to the followers and the mentioned users.
  5. Retweet - User can perform retweet using this option. It will show top 20 latest tweets either from the users that the logged in user is following or the tweets that the logged in user was tagged in. User can select any one of the tweet and retweet it.
  6. Notifications - This will show all the notifications which include tweets posted by followers, retweets, tweets the logged in user is mentioned in, new user starts following logged in user etc.
  7. Exit - Exit the application.
  
  
##How to run the project:

There are two subdirectories under the source_code directory. First navigate to the Server directory and execute the following command to start the server process.

    dotnet run

After this, navigate to the Client directory and execute the following command.

    dotnet run

This will start the client process. On client console window, you will two options - Start Simulator and Open Shell. You can watch the attached demo video to see the working of these two options.
Please note that we have tested our code with .net version 5.


## Demo video link
