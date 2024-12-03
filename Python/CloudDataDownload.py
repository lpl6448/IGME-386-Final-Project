import requests
from bs4 import BeautifulSoup
from datetime import datetime, timezone
import os

# Base URL for the RAP model data
base_url = "https://nomads.ncep.noaa.gov/pub/data/nccf/com/rap/prod/rap.20241203/"

# Directory to save the file
save_directory = "Data/Unzipped"

def get_current_zulu_hour():
    """Return the current hour in UTC as an integer."""
    return int(datetime.now(timezone.utc).hour)

def find_and_download_closest_file():
    current_hour = get_current_zulu_hour()
    
    # Get the index page
    response = requests.get(base_url)
    if response.status_code != 200:
        print(f"Failed to access {base_url}")
        return

    # Parse the HTML content
    soup = BeautifulSoup(response.text, 'html.parser')
    links = soup.find_all('a')

    available_hours = []
    # Extract all available tXXz values from the links
    for link in links:
        href = link.get('href')
        if href and ".awip32f01.grib" in href:
            hour_str = href.split(".")[1][1:3]
            if hour_str.isdigit():
                available_hours.append(int(hour_str))

    if not available_hours:
        print("No available files found.")
        return

    # Find the closest hour
    closest_hour = min(available_hours, key=lambda x: abs(x - current_hour))
    target_filename = f"rap.t{str(closest_hour).zfill(2)}z.awip32f01.grib2"
    
    # Download the closest file
    for link in links:
        href = link.get('href')
        if href and target_filename in href:
            download_url = base_url + href
            download_file(download_url, target_filename)
            break
    else:
        print(f"File {target_filename} not found.")

def download_file(url, filename):
    """Download the file from the given URL and save it in the Data directory."""
    if not os.path.exists(save_directory):
        os.makedirs(save_directory)

    filepath = os.path.join(save_directory, filename)
    print(f"Downloading {filename} from {url}...")

    response = requests.get(url, stream=True)
    if response.status_code == 200:
        with open(filepath, 'wb') as file:
            for chunk in response.iter_content(chunk_size=1024):
                file.write(chunk)
        print(f"Download completed: {filepath}")
    else:
        print(f"Failed to download {filename}")

if __name__ == "__main__":
    find_and_download_closest_file()
