<!DOCTYPE html>
<?php
if ($_SERVER['REQUEST_METHOD'] == 'POST') {
    // Check if file was uploaded without errors
    if (isset($_FILES["file-upload"]) && $_FILES["file-upload"]["error"] == UPLOAD_ERR_OK) {
        $allowed = ["jpg" => "image/jpeg", "png" => "image/png", "gif" => "image/gif"];
        $filename = $_FILES["file-upload"]["name"];
        $filetype = $_FILES["file-upload"]["type"];
        $filesize = $_FILES["file-upload"]["size"];

        // Verify file extension
        $ext = pathinfo($filename, PATHINFO_EXTENSION);
        if (!array_key_exists($ext, $allowed)) {
            echo "Error: Please select a valid file format.";
            exit;
        }

        // Verify file size - 10MB maximum
        $maxsize = 10 * 1024 * 1024;
        if ($filesize > $maxsize) {
            echo "Error: File size is larger than the allowed limit: 10MB";
            exit;
        }

        // Verify MYME type of the file
        if (in_array($filetype, $allowed)) {
            // Check whether file exists before uploading it
            $destination = "upload/" . $filename;
            if (file_exists($destination)) {
                echo $filename . " already exists.";
            } else {
                // Use move_uploaded_file instead of copy to prevent script execution
                if (move_uploaded_file($_FILES["file-upload"]["tmp_name"], $destination)) {
                    echo "<p id='upload-success'>Your file was uploaded successfully.</p>";
                } else {
                    echo "Error: There was a problem moving your file.";
                }
            } 
        } else {
            echo "Error: There was a problem uploading your file. Please try again."; 
        }
    } else {
        echo "Error: " . $_FILES["file-upload"]["error"];
    }
}
?>
<html>
<body>

<h2>Upload File</h2>

<form action="<?php echo $_SERVER['PHP_SELF']; ?>" method="post" enctype="multipart/form-data">
  <label for="file-upload">Choose file:</label>
  <input type="file" id="file-upload" name="file-upload"><br><br>
  <input type="submit" id="upload-button" value="Upload">
</form>

</body>
</html>
