<?php
$dbServername = "sql300.infinityfree.com";
$dbUsername = "if0_36162692";
$dbPassword = "sitemm728";
$dbName = "if0_36162692_database";

try {
    // Create connection
    $dbConn = new mysqli($dbServername, $dbUsername, $dbPassword, $dbName);

    // Check connection
    if ($dbConn->connect_error) {
        throw new Exception("Connection failed: " . $dbConn->connect_error);
    }

    // Validate computer_id
    if (!isset($_GET['computerId'])) {
        // 400 Bad Request
        throw new Exception("Invalid computer_id", 400);
    }
    $computerId = $_GET['computerId'];

    $selectSql = "SELECT * FROM MTB2Commands WHERE status = 'pending' AND computer_id = ? ORDER BY received_at ASC";
    $selectStmt = $dbConn->prepare($selectSql);
    if (!$selectStmt) {
        throw new Exception("Failed to prepare statement: " . $dbConn->error);
    }
    $selectStmt->bind_param("i", $computerId);
    $selectResult = $selectStmt->execute();
    if (!$selectResult) {
        throw new Exception("Failed to execute statement: " . $selectStmt->error);
    }

    $commandList = [];
    $selectResult = $selectStmt->get_result();
    if ($selectResult->num_rows > 0) {
        while ($row = $selectResult->fetch_assoc()) {
            // Decode the parameters field into an associative array
            $row['parameters'] = json_decode($row['parameters'], true);
            $commandList[] = $row;
        }
    }
    else {
        echo json_encode(['message' => "No commands found"], JSON_PRETTY_PRINT);
        return;
    }

    $updateSql = "UPDATE MTB2Commands SET status = 'delivered' WHERE status = 'pending' AND computer_id = ?";
    $updateStmt = $dbConn->prepare($updateSql);
    if (!$updateStmt) {
        throw new Exception("Failed to prepare statement: " . $dbConn->error);
    }
    $updateStmt->bind_param("i", $computerId);
    $updateResult = $updateStmt->execute();
    if (!$updateResult) {
        throw new Exception("Failed to execute statement: " . $updateStmt->error);
    }

/* Echo data in JSON format (not necessarily with the indentation shown here, just easier to read):

[
    {
        "command": "string",
        "parameters": {
            "key": "value"
        }, // object? parameters = null
        "response": "string", // string? response = null
        "status": "string", // Enum
        "id": int,
        "computerId": "string",
        "ReceivedAt": "DD-MM-YYYY HH:MM:SS"
    }
    // Optional additional commands...
]
*/

    echo json_encode($commandList, JSON_PRETTY_PRINT);

} catch (Exception $e) {
    // Handle exception
    // Check if response code is given in the exception object
    if ($e->getCode() >= 200 && $e->getCode() < 600) {
        $response_code = $e->getCode();
    } else {
        // 500 Internal Server Error
        $response_code = 500;
    }
    http_response_code($response_code);
    echo json_encode(['error' => $e->getMessage(), 'code' => $response_code], JSON_PRETTY_PRINT);
} finally {
    // Always close the connection
    if ($dbConn) {
        $dbConn->close();
    }
}