<?php
// database credentials
$dbServername = "sql300.infinityfree.com";
$dbUsername = "if0_36162692";
$dbPassword = "sitemm728";
$dbName = "if0_36162692_database";




// Get the data from the client from the GET request
$id = $_GET['id'] ?? null;
$response = $_GET['response'] ?? null;
$status = $_GET['status'] ?? null;

// Input validation
if (empty($id) || empty($response)) {
	throw new Exception("Invalid input");
}

// Url decode response
$response = urldecode($response);

// Connect to the database
$conn = new mysqli($dbServername, $dbUsername, $dbPassword, $dbName);
// Check if the connection was successful
if ($conn->connect_error) {
	throw new Exception("Connection failed: " . $conn->connect_error);
}

// Prepare the SQL statement
$sql = "UPDATE MTB2Commands SET response = ?";
$params = [$response];

if (!empty($status)) {
	$sql .= ", status = ?";
	$params[] = $status;
}

$sql .= " WHERE id = ?";
$params[] = $id;

$stmt = $conn->prepare($sql);
if ($stmt === false) {
	throw new Exception("Failed to prepare statement: " . $conn->error);
}

// Bind the parameters to the SQL statement
$types = str_repeat("s", count($params) - 1) . "i"; // Use "i" for the last parameter ($id)
if (!$stmt->bind_param($types, ...$params)) {
	throw new Exception("Failed to bind parameters: " . $stmt->error);
}


// Execute the SQL statement
if (!$stmt->execute()) {
	throw new Exception("Failed to execute statement: " . $stmt->error);
}

// Check if the SQL statement was executed successfully
if ($stmt->affected_rows > 0) {
	echo "Response updated successfully (C# keywords: OK success true 1 )";
} else {
	echo "Failed to update response (C# keywords: failed failure false 0 error -1 )";
}

// Close the prepared statement
$stmt->close();
// Close the database connection
$conn->close();