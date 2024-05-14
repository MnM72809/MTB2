<?php

$directory = 'files';

// Get the list of files in the directory
$files = scandir($directory);

// Remove the "." and ".." entries from the list
$files = array_diff($files, array('.', '..'));

// Convert the file list to JSON
$indexedFiles = array_values($files);
$json = json_encode($indexedFiles);

// Set the response content type to JSON
// Line commented out because if not, the browser adds elements that makes the output invalid JSON
//header('Content-Type: application/json');

// Echo the JSON response
echo $json;