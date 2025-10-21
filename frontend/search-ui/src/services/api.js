import axios from 'axios';

const searchApiUrl = import.meta.env.VITE_SEARCH_API_URL || 'http://localhost:8081';
const ingestionApiUrl = import.meta.env.VITE_INGESTION_API_URL || 'http://localhost:8080';

const searchClient = axios.create({
  baseURL: searchApiUrl,
  timeout: 20000
});

const ingestionClient = axios.create({
  baseURL: ingestionApiUrl,
  timeout: 20000
});

export const searchRecords = async ({ query, page, pageSize, dataset, sort }) => {
  const response = await searchClient.get('/api/search', {
    params: {
      query,
      page,
      pageSize,
      dataset,
      sort
    }
  });

  return response.data;
};

export const fetchDatasets = async () => {
  const response = await searchClient.get('/api/datasets');
  return response.data;
};

export const createManualRecord = async ({ dataset, fields }) => {
  const response = await ingestionClient.post(`/api/datasets/${encodeURIComponent(dataset)}/records`, {
    fields
  });
  return response.data;
};

export const uploadDatasetFile = async ({ dataset, file }) => {
  const formData = new FormData();
  formData.append('file', file);
  const response = await ingestionClient.post(`/api/datasets/${encodeURIComponent(dataset)}/upload`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  });
  return response.data;
};
