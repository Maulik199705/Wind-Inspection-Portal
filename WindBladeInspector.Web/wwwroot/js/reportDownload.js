window.downloadFileFromStream = async (fileName, contentType, contentStreamReference) => {
    // Read the stream from .NET into an ArrayBuffer
    const arrayBuffer = await contentStreamReference.arrayBuffer();

    // Create a blob URL for the file, specifying the content type
    const blob = new Blob([arrayBuffer], { type: contentType });
    const url = URL.createObjectURL(blob);

    // Create a temporary anchor element and trigger the download
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? 'report.pdf';
    anchorElement.click();

    // Clean up
    anchorElement.remove();
    URL.revokeObjectURL(url);
};