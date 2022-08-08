let index = 0;

function AddTag() {
    // Get a reference to the TagEntry input element
    let tagEntry = document.getElementById("TagEntry");

    // Create a new Select Option
    let newOption = new Option(tagEntry.value, tagEntry.value);
    document.getElementById("TagList").options[index++] = newOption;

    // Clear out the TagEntry control
    tagEntry.value = "";
    return true;
}

function DeleteTag() {
    let tagCount = 1;
    let tagList = document.getElementById("TagList");
    while (tagCount > 0) {
        let selectedIndex = tagList.selectedIndex;
        if (selectedIndex >= 0) {
            tagList.options[selectedIndex] = null;
            --tagCount;
            index--;
        } else {
            tagCount = 0;
            index--;
        }
    }

}