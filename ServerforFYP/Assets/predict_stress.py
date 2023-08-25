import sys
import json
import joblib
import pandas as pd
import numpy as np
from scipy import signal
import gcsfs
from google.oauth2 import service_account

def predict_stress(single_df):
    # Load and preprocess the single data
    # single_df = pd.read_csv(file_path)
    selected_columns = single_df.columns[:8].tolist() + single_df.columns[-2:].tolist()
    ranked_single_df = single_df[selected_columns].copy()

    freq_bands = {'alpha': (8, 13),
                  'fast_beta': (13, 30)}

    # Calculate PSD and extract features
    single_freqs, single_psd = signal.welch(ranked_single_df.iloc[:, :8], fs=64, nperseg=128, scaling='density')
    single_psd_dict = {}
    for band, (fmin, fmax) in freq_bands.items():
        freq_idx = np.where((fmin <= single_freqs) & (single_freqs < fmax))[0]
        psd_band = np.mean(single_psd[:, freq_idx], axis=1)
        single_psd_dict[band] = psd_band

    single_features = pd.DataFrame(single_psd_dict)

    # Calculate stressed percentage for the single data
    baseline_mean_alpha_single = single_features['alpha'].mean()
    baseline_mean_fast_beta_single = single_features['fast_beta'].mean()

    single_features['Stressed Percentage'] = (
        (single_features['alpha'] - baseline_mean_alpha_single) / baseline_mean_alpha_single +
        (single_features['fast_beta'] - baseline_mean_fast_beta_single) / baseline_mean_fast_beta_single
    ) / 2 * 100

    # Prepare input features for prediction
    single_X = single_features.drop(['Stressed Percentage'], axis=1)

    # Load the trained kNN regressor
    model_filename = 'D:\FYP\Server\ServerforFYP\ServerforFYP\Assets\eeg_knn_model.pkl'
    loaded_knn_regressor = joblib.load(model_filename)

    # Make predictions using the trained kNN regressor
    single_predicted_percentage = loaded_knn_regressor.predict(single_X)

    return json.dumps(single_predicted_percentage.tolist())

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python script.py <gcs_file_path>")
        sys.exit(1)

    gcs_file_path = sys.argv[1]
    credentials = service_account.Credentials.from_service_account_file('D:/FYP/Server/fypbackend1-d97d2809d29a.json', scopes=['https://www.googleapis.com/auth/cloud-platform'])

    # Read the CSV file directly from Google Cloud Storage using service account key
    fs = gcsfs.GCSFileSystem(project='fypbackend1', token=credentials)
    with fs.open(gcs_file_path, 'rb') as gcs_file:
        single_df = pd.read_csv(gcs_file)

    single_predicted_percentage = predict_stress(single_df)
    print(single_predicted_percentage)
    sys.stdout.flush()