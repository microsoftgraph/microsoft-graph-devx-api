-- Use the below in Postman or any other tool to test the web api

-- BEGIN OF GET REQUESTS

-----------
-- Headers
-----------
Content-Type: text/message

-------------------------------------------------
-- Body
-- example with select, filter, orderby and skip
-------------------------------------------------
GET /v1.0/me/messages?$select=subject,IsRead,sender,toRecipients&$filter=IsRead%20eq%20false&$orderby=subject%20desc&$skip=10 HTTP/1.1
Host: graph.microsoft.com
Accept: text/message


-- END OF GET REQUESTS
