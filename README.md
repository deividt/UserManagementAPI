## User Management API

This api is a user management api, part of the backend of the Microsoft Backend Training.

### Prompt used to generate the API endpoints

```
In my UsersController, add the endpoints for the following operations:

    GET: Retrieve a list of users or a specific user by ID.

    POST: Add a new user.

    PUT: Update an existing user's details.

    DELETE: Remove a user by ID.


Also create a User object to represent our user, that should have id, name and e-mail as properties.

Use the best practices and use a static list to store the users.

```

### Prompts used to help idenfity errors and improve code quality

```
Can you see if there is any errors on Users were being added without proper validation and fix them?
```

```
Great, what about Errors occurred when retrieving non-existent users?
```

```
Is there anything that can be done in order to improve the GetUsers endpoint?
```

### Prompts used to implement the middleware pipeline


```
 write a middleware that logs:

    HTTP method (e.g., GET, POST).

    Request path.

    Response status code.
```

```
Create a middleware that:

    Catches unhandled exceptions.

    Return consistent error responses in JSON format (e.g., { "error": "Internal server error." }).
```

```
Implement a middleware that:
            
- Validates tokens from incoming requests.
            
- Allows access only to users with valid tokens.
            
- Returns a 401 Unauthorized response for invalid tokens.
```

```
I have removed the validation for development endpoints. Now, how can i test it in the swagger?
```

```
I'm testing using swagger, but even i have authenticated, is returning `"No token provided in request"`, can you help me solve it?
```


