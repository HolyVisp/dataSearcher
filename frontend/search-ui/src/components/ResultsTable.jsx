import PropTypes from 'prop-types';
import dayjs from 'dayjs';
import { Button, Image, Table, Tag, Tooltip } from 'antd';
import { FilePdfOutlined } from '@ant-design/icons';

function isImageValue(value) {
  if (typeof value !== 'string') {
    return false;
  }

  const trimmed = value.trim();
  if (trimmed.startsWith('data:image')) {
    return true;
  }

  return /(https?:\/\/.*\.(?:png|jpe?g|gif|webp|svg))/i.test(trimmed);
}

function renderFieldValue(value) {
  if (Array.isArray(value)) {
    return value.join(', ');
  }

  if (isImageValue(value)) {
    return <Image src={value} width={120} alt="record" />;
  }

  if (value === null || value === undefined || value === '') {
    return '—';
  }

  return value.toString();
}

export function ResultsTable({ dataSource, loading, pagination, onChange, onExport, total }) {
  const columns = [
    {
      title: 'Датасет',
      dataIndex: 'dataset',
      key: 'dataset',
      sorter: true,
      render: (dataset) => <Tag color="blue">{dataset}</Tag>
    },
    {
      title: 'Индекс',
      dataIndex: 'index',
      key: 'index',
      ellipsis: true
    },
    {
      title: 'Релевантность',
      dataIndex: 'score',
      key: 'score',
      sorter: true,
      width: 140,
      render: (score) => (score ? score.toFixed(3) : '—')
    },
    {
      title: 'Создано',
      dataIndex: 'createdAt',
      key: 'createdAt',
      sorter: true,
      width: 200,
      render: (value) => (value ? dayjs(value).format('DD.MM.YYYY HH:mm') : '—')
    }
  ];

  return (
    <div className="results-table">
      <div className="results-header">
        <div className="results-info">Найдено записей: {total}</div>
        <Tooltip title="Экспортировать текущую страницу в PDF">
          <Button icon={<FilePdfOutlined />} onClick={onExport} disabled={!dataSource.length}>
            Экспорт в PDF
          </Button>
        </Tooltip>
      </div>
      <Table
        rowKey={(record) => record.id}
        columns={columns}
        dataSource={dataSource}
        loading={loading}
        pagination={pagination}
        onChange={onChange}
        expandable={{
          expandedRowRender: (record) => (
            <div className="record-fields">
              {Object.entries(record.fields).map(([key, value]) => (
                <div className="record-field" key={key}>
                  <label>{key}</label>
                  <span>{renderFieldValue(value)}</span>
                </div>
              ))}
            </div>
          )
        }}
      />
    </div>
  );
}

ResultsTable.propTypes = {
  dataSource: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.string.isRequired,
      dataset: PropTypes.string.isRequired,
      index: PropTypes.string,
      fields: PropTypes.object.isRequired,
      score: PropTypes.number,
      createdAt: PropTypes.string
    })
  ),
  loading: PropTypes.bool,
  pagination: PropTypes.object.isRequired,
  onChange: PropTypes.func.isRequired,
  onExport: PropTypes.func.isRequired,
  total: PropTypes.number.isRequired
};

ResultsTable.defaultProps = {
  dataSource: [],
  loading: false
};

export default ResultsTable;
